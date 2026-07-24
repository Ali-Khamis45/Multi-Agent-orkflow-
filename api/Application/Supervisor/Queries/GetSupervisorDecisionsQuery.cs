using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Supervisor.Queries;

public sealed record SupervisorDecisionDto(
    Guid Id, Guid WorkflowRunId, Guid CorrelationId, string DecisionType, string InputSnapshotJson,
    string Rationale, double Confidence, string? TargetNodeIdsJson, DateTimeOffset CreatedAt);

/// <summary>Powers the Supervisor Brain page (Phase 1.6) — the full decision log for a run, oldest first.</summary>
public sealed record GetSupervisorDecisionsQuery(Guid WorkflowRunId) : IRequest<IReadOnlyCollection<SupervisorDecisionDto>>;

public sealed class GetSupervisorDecisionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSupervisorDecisionsQuery, IReadOnlyCollection<SupervisorDecisionDto>>
{
    public async Task<IReadOnlyCollection<SupervisorDecisionDto>> Handle(
        GetSupervisorDecisionsQuery request, CancellationToken cancellationToken)
    {
        return await db.SupervisorDecisions
            .Where(d => d.WorkflowRunId == request.WorkflowRunId)
            .OrderBy(d => d.CreatedAt)
            .Select(d => new SupervisorDecisionDto(
                d.Id, d.WorkflowRunId, d.CorrelationId, d.DecisionType.ToString(), d.InputSnapshotJson,
                d.Rationale, d.Confidence, d.TargetNodeIdsJson, d.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
