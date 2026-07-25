using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Reasoning.Queries;

public sealed record ReasoningStageMetricDto(
    string Stage, int Count, double AvgDurationMs, double? AvgConfidence,
    long TotalTokens, int TotalToolCalls, int TotalMemoryReads, int TotalMemoryWrites, int TotalRetries);

public sealed record ReasoningTracePointDto(
    DateTimeOffset At, string Stage, string Agent, double? Confidence, long DurationMs, int? Tokens, Guid WorkflowRunId);

public sealed record ReasoningTelemetryDto(
    IReadOnlyCollection<ReasoningStageMetricDto> StageMetrics, IReadOnlyCollection<ReasoningTracePointDto> RecentPoints);

/// <summary>
/// Workspace-wide reasoning aggregate for the Telemetry Center
/// (ARCHITECTURE_EXTENSION.md §E11) — per-stage duration/confidence/token
/// rollups plus a bounded recent-points feed the frontend turns into
/// distribution and timeline charts without a new endpoint per chart.
/// </summary>
public sealed record GetReasoningTelemetryQuery(Guid WorkspaceId, int PointsLimit = 300)
    : IRequest<ReasoningTelemetryDto>;

public sealed class GetReasoningTelemetryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReasoningTelemetryQuery, ReasoningTelemetryDto>
{
    public async Task<ReasoningTelemetryDto> Handle(GetReasoningTelemetryQuery request, CancellationToken cancellationToken)
    {
        var runIds = db.WorkflowRuns
            .Where(r => r.WorkspaceId == request.WorkspaceId)
            .Select(r => r.Id);

        var scoped = db.ReasoningTraces.Where(t => runIds.Contains(t.WorkflowRunId));

        var stageMetrics = await scoped
            .GroupBy(t => t.Stage)
            .Select(g => new ReasoningStageMetricDto(
                g.Key.ToString(),
                g.Count(),
                g.Average(t => (double)t.DurationMs),
                g.Any(t => t.Confidence != null) ? g.Where(t => t.Confidence != null).Average(t => t.Confidence!.Value) : null,
                g.Sum(t => (long)(t.Tokens ?? 0)),
                g.Sum(t => t.ToolCalls),
                g.Sum(t => t.MemoryReads),
                g.Sum(t => t.MemoryWrites),
                g.Sum(t => t.RetryCount)))
            .ToListAsync(cancellationToken);

        var recentPoints = await scoped
            .OrderByDescending(t => t.StartedAt)
            .Take(request.PointsLimit)
            .Select(t => new ReasoningTracePointDto(
                t.StartedAt, t.Stage.ToString(), t.Agent, t.Confidence, t.DurationMs, t.Tokens, t.WorkflowRunId))
            .ToListAsync(cancellationToken);

        return new ReasoningTelemetryDto(stageMetrics, recentPoints);
    }
}
