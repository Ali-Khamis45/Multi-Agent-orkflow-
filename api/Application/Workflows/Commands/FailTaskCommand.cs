using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Application.Scheduling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workflows.Commands;

/// <summary>
/// Reaction to a <c>TaskFailed</c> event (ARCHITECTURE.md §10 Self-Healing). Retries
/// up to a fixed default policy (2 attempts) by resetting the node to Pending so the
/// next scheduling pass re-resolves a candidate agent — possibly a different one, if
/// the original is now unavailable or busy. Exhausted retries fail the whole run for
/// Phase 1; branching around non-critical failures is deferred (build order §24).
/// </summary>
public sealed record FailTaskCommand(Guid TaskNodeId, string? ReasonJson) : IRequest;

public sealed class FailTaskCommandHandler(IApplicationDbContext db, IEventBus eventBus, ISchedulerService scheduler)
    : IRequestHandler<FailTaskCommand>
{
    private const int MaxAttempts = 2;

    public async Task Handle(FailTaskCommand request, CancellationToken cancellationToken)
    {
        var workflowRunId = await db.TaskNodes
            .Where(n => n.Id == request.TaskNodeId)
            .Select(n => n.WorkflowRunId)
            .FirstOrDefaultAsync(cancellationToken);

        var run = await db.WorkflowRuns
            .Include(r => r.Nodes)
            .Include(r => r.Edges)
            .FirstOrDefaultAsync(r => r.Id == workflowRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowRun for task {request.TaskNodeId} not found.");

        var node = run.Nodes.First(n => n.Id == request.TaskNodeId);

        if (node.AssignedAgentName is not null)
        {
            var agent = await db.AgentRegistrations
                .FirstOrDefaultAsync(a => a.Name == node.AssignedAgentName, cancellationToken);
            agent?.DecrementInFlight();
        }

        node.Fail(request.ReasonJson);

        if (node.AttemptCount < MaxAttempts)
        {
            node.ResetForRetry();
            await scheduler.ScheduleReadyNodesAsync(run.Id, cancellationToken);
            return;
        }

        run.Fail();
        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.WorkflowRunFailed,
            WorkspaceId = run.WorkspaceId,
            WorkflowRunId = run.Id,
            TaskId = node.Id,
            ProducedBy = "scheduler",
            PayloadJson = JsonSerializer.Serialize(new { run.Id, FailedNodeId = node.Id, node.Name })
        }, cancellationToken);
    }
}
