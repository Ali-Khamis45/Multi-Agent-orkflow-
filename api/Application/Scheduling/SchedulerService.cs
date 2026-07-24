using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Domain.Agents;
using AiAgentsTeam.Domain.Checkpoints;
using AiAgentsTeam.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiAgentsTeam.Application.Scheduling;

public sealed class SchedulerService(IApplicationDbContext db, IEventBus eventBus, ILogger<SchedulerService> logger)
    : ISchedulerService
{
    public async Task ScheduleReadyNodesAsync(Guid workflowRunId, CancellationToken cancellationToken = default)
    {
        var run = await db.WorkflowRuns
            .Include(r => r.Nodes)
            .Include(r => r.Edges)
            .FirstOrDefaultAsync(r => r.Id == workflowRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowRun {workflowRunId} not found.");

        // §5.2 step 1: compute the ready set (Pending nodes whose every predecessor is Completed).
        var readyNodes = run.ComputeReadySet();

        // §5.2 step 3: dispatch every ready node concurrently — this is what makes
        // independent branches (e.g. Backend + Frontend) execute in parallel: they
        // simply land in the same ready set with no edge between them.
        foreach (var node in readyNodes)
        {
            await TryDispatchAsync(run, node, cancellationToken);
        }

        var label = "scheduling-pass";

        if (run.IsComplete())
        {
            run.Complete();
            label = "workflow-completed";
            await eventBus.PublishAsync(new EventEnvelope
            {
                Type = EventTypes.WorkflowRunCompleted,
                WorkspaceId = run.WorkspaceId,
                WorkflowRunId = run.Id,
                CorrelationId = run.CorrelationId,
                ProducedBy = "scheduler",
                PayloadJson = JsonSerializer.Serialize(new { WorkflowRunId = run.Id })
            }, cancellationToken);
        }

        // Execution Snapshots (Phase 1.5 §5): a lightweight Checkpoint after every
        // scheduling pass — enough for future Resume/Replay/Debugging to read this
        // table directly, no storage redesign needed.
        db.Checkpoints.Add(BuildCheckpoint(run, label));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Checkpoint BuildCheckpoint(WorkflowRun run, string label)
    {
        var snapshot = new
        {
            run.Id,
            run.WorkspaceId,
            Status = run.Status.ToString(),
            Nodes = run.Nodes.Select(n => new
            {
                n.Id,
                n.Name,
                n.TaskType,
                Status = n.Status.ToString(),
                n.AssignedAgentName,
                n.Confidence,
                n.RiskLevel,
                n.AttemptCount
            }),
            Edges = run.Edges.Select(e => new { e.PredecessorNodeId, e.SuccessorNodeId }),
            SnapshottedAt = DateTimeOffset.UtcNow
        };

        return new Checkpoint(run.Id, run.CorrelationId, label, JsonSerializer.Serialize(snapshot));
    }

    private async Task TryDispatchAsync(WorkflowRun run, TaskNode node, CancellationToken cancellationToken)
    {
        // §5.2 step 2: resolve candidates by supportedTasks, filter by availability,
        // rank by priority then current load (fewest in-flight tasks).
        var candidate = await db.AgentRegistrations
            .Where(a => a.Status == AgentStatus.Available)
            .ToListAsync(cancellationToken);

        var resolved = candidate
            .Where(a => a.Supports(node.TaskType))
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.InFlightTaskCount)
            .FirstOrDefault();

        if (resolved is null)
        {
            logger.LogWarning(
                "No available agent supports task type {TaskType} for node {NodeId}; leaving Pending.",
                node.TaskType, node.Id);
            return;
        }

        node.Dispatch(resolved.Name);
        resolved.IncrementInFlight();

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.TaskDispatched,
            WorkspaceId = run.WorkspaceId,
            WorkflowRunId = node.WorkflowRunId,
            TaskId = node.Id,
            CorrelationId = run.CorrelationId,
            ProducedBy = "scheduler",
            PayloadJson = JsonSerializer.Serialize(new
            {
                TaskNodeId = node.Id,
                node.WorkflowRunId,
                node.TaskType,
                node.Name,
                InputsJson = node.InputsJson,
                AssignedAgent = resolved.Name,
                AgentEndpoint = resolved.Endpoint,
                node.AttemptCount
            })
        }, cancellationToken);
    }
}
