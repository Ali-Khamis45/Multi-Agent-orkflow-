using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Domain.Supervisor;
using MediatR;

namespace AiAgentsTeam.Application.Supervisor.Commands;

/// <summary>
/// Audit trail for the Supervisor Agent's executive decisions (ARCHITECTURE_EXTENSION.md
/// §E1) — e.g. "expand DAG post-BA", "retry node X on a different agent". This never
/// mutates workflow state itself; the Supervisor still calls the ordinary Workflow
/// commands (AddTaskNode, StartWorkflowRun, ...) to act. This is the "why," logged
/// separately from the "what." CorrelationId (Phase 1.5 §2) is always derived
/// server-side from the parent WorkflowRun.
/// </summary>
public sealed record RecordSupervisorDecisionCommand(
    Guid WorkflowRunId,
    SupervisorDecisionType DecisionType,
    string InputSnapshotJson,
    string Rationale,
    double Confidence,
    string? TargetNodeIdsJson) : IRequest<Guid>;

public sealed class RecordSupervisorDecisionCommandHandler(IApplicationDbContext db, IEventBus eventBus)
    : IRequestHandler<RecordSupervisorDecisionCommand, Guid>
{
    public async Task<Guid> Handle(RecordSupervisorDecisionCommand request, CancellationToken cancellationToken)
    {
        var run = await db.WorkflowRuns.FindAsync([request.WorkflowRunId], cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowRun {request.WorkflowRunId} not found.");

        var decision = new SupervisorDecision(
            request.WorkflowRunId, run.CorrelationId, request.DecisionType, request.InputSnapshotJson,
            request.Rationale, request.Confidence, request.TargetNodeIdsJson);

        db.SupervisorDecisions.Add(decision);
        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.SupervisorDirectiveIssued,
            WorkspaceId = run.WorkspaceId,
            WorkflowRunId = request.WorkflowRunId,
            CorrelationId = run.CorrelationId,
            ProducedBy = "supervisor",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                DecisionId = decision.Id,
                request.DecisionType,
                request.Rationale
            })
        }, cancellationToken);

        return decision.Id;
    }
}
