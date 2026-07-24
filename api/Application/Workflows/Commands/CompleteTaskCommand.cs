using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Scheduling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workflows.Commands;

/// <summary>
/// Reaction to a <c>TaskCompleted</c> event published by the AI Runtime (consumed by
/// the Infrastructure-layer Event Bus consumer, which Sends this command). Updates
/// domain state, frees the agent's in-flight slot, then re-triggers the Scheduler
/// (§5.2 step 4: recompute the ready set on every status change).
/// </summary>
public sealed record CompleteTaskCommand(
    Guid TaskNodeId,
    string OutputsJson,
    double Confidence,
    string RiskLevel,
    string? ReasoningSummary) : IRequest;

public sealed class CompleteTaskCommandHandler(IApplicationDbContext db, ISchedulerService scheduler)
    : IRequestHandler<CompleteTaskCommand>
{
    public async Task Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
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

        node.Complete(request.OutputsJson, request.Confidence, request.RiskLevel, request.ReasoningSummary);

        // Persists this completion together with whatever the scheduler dispatches next.
        await scheduler.ScheduleReadyNodesAsync(run.Id, cancellationToken);
    }
}
