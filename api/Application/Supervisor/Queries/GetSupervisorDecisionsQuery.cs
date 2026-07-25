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

public sealed record DecisionTypeCountDto(string DecisionType, int Count);

public sealed record SupervisorSummaryDto(
    IReadOnlyCollection<DecisionTypeCountDto> Counts, IReadOnlyCollection<SupervisorDecisionDto> Recent);

/// <summary>Workspace-wide Supervisor Brain feed (Phase 1.6) — every decision
/// across every run, for the Supervisor page and the Telemetry Center's
/// decision-type breakdown.</summary>
public sealed record GetSupervisorSummaryQuery(Guid WorkspaceId, int Limit = 100) : IRequest<SupervisorSummaryDto>;

public sealed class GetSupervisorSummaryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetSupervisorSummaryQuery, SupervisorSummaryDto>
{
    public async Task<SupervisorSummaryDto> Handle(GetSupervisorSummaryQuery request, CancellationToken cancellationToken)
    {
        var runIds = db.WorkflowRuns.Where(r => r.WorkspaceId == request.WorkspaceId).Select(r => r.Id);
        var scoped = db.SupervisorDecisions.Where(d => runIds.Contains(d.WorkflowRunId));

        var counts = await scoped
            .GroupBy(d => d.DecisionType)
            .Select(g => new DecisionTypeCountDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        var recent = await scoped
            .OrderByDescending(d => d.CreatedAt)
            .Take(request.Limit)
            .Select(d => new SupervisorDecisionDto(
                d.Id, d.WorkflowRunId, d.CorrelationId, d.DecisionType.ToString(), d.InputSnapshotJson,
                d.Rationale, d.Confidence, d.TargetNodeIdsJson, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return new SupervisorSummaryDto(counts, recent);
    }
}
