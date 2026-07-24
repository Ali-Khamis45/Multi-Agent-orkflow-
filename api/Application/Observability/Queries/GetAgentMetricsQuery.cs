using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Workflow;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Observability.Queries;

/// <summary>
/// Agent Metrics (Phase 1.5 §6) — computed from data every agent invocation
/// already produces (TaskNode outcomes + ReasoningTrace telemetry), not a
/// separate tracked-metrics system. "These metrics should already exist before
/// the dashboard" — this query is that existence proof.
/// </summary>
public sealed record AgentMetricsDto(
    string AgentName,
    int TotalTasks,
    int CompletedTasks,
    int FailedTasks,
    double SuccessRate,
    double FailureRate,
    double AvgAttemptCount,
    double? AvgConfidence,
    double AvgStageDurationMs,
    int ToolCallCount,
    int MemoryReadCount,
    int MemoryWriteCount,
    IReadOnlyDictionary<string, int> ModelUsage);

public sealed record GetAgentMetricsQuery(string? AgentName = null) : IRequest<IReadOnlyCollection<AgentMetricsDto>>;

public sealed class GetAgentMetricsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentMetricsQuery, IReadOnlyCollection<AgentMetricsDto>>
{
    public async Task<IReadOnlyCollection<AgentMetricsDto>> Handle(GetAgentMetricsQuery request, CancellationToken cancellationToken)
    {
        var nodesQuery = db.TaskNodes.Where(n => n.AssignedAgentName != null);
        if (request.AgentName is not null)
            nodesQuery = nodesQuery.Where(n => n.AssignedAgentName == request.AgentName);

        var nodeStats = await nodesQuery
            .GroupBy(n => n.AssignedAgentName!)
            .Select(g => new
            {
                AgentName = g.Key,
                TotalTasks = g.Count(),
                CompletedTasks = g.Count(n => n.Status == TaskNodeStatus.Completed),
                FailedTasks = g.Count(n => n.Status == TaskNodeStatus.Failed),
                AvgAttemptCount = g.Average(n => n.AttemptCount),
                AvgConfidence = g.Where(n => n.Confidence != null).Average(n => (double?)n.Confidence)
            })
            .ToListAsync(cancellationToken);

        var tracesQuery = db.ReasoningTraces.AsQueryable();
        if (request.AgentName is not null)
            tracesQuery = tracesQuery.Where(t => t.Agent == request.AgentName);

        var traceStats = await tracesQuery
            .GroupBy(t => t.Agent)
            .Select(g => new
            {
                AgentName = g.Key,
                AvgStageDurationMs = g.Average(t => (double)t.DurationMs),
                ToolCallCount = g.Sum(t => t.ToolCalls),
                MemoryReadCount = g.Sum(t => t.MemoryReads),
                MemoryWriteCount = g.Sum(t => t.MemoryWrites)
            })
            .ToListAsync(cancellationToken);

        var modelUsage = await tracesQuery
            .Where(t => t.ModelUsed != null)
            .GroupBy(t => new { t.Agent, t.ModelUsed })
            .Select(g => new { g.Key.Agent, Model = g.Key.ModelUsed!, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var agentNames = nodeStats.Select(n => n.AgentName)
            .Union(traceStats.Select(t => t.AgentName))
            .Distinct();

        return agentNames.Select(name =>
        {
            var n = nodeStats.FirstOrDefault(x => x.AgentName == name);
            var t = traceStats.FirstOrDefault(x => x.AgentName == name);
            var total = n?.TotalTasks ?? 0;
            var models = modelUsage.Where(m => m.Agent == name).ToDictionary(m => m.Model, m => m.Count);

            return new AgentMetricsDto(
                name,
                total,
                n?.CompletedTasks ?? 0,
                n?.FailedTasks ?? 0,
                total == 0 ? 0 : (double)(n!.CompletedTasks) / total,
                total == 0 ? 0 : (double)(n!.FailedTasks) / total,
                n?.AvgAttemptCount ?? 0,
                n?.AvgConfidence,
                t?.AvgStageDurationMs ?? 0,
                t?.ToolCallCount ?? 0,
                t?.MemoryReadCount ?? 0,
                t?.MemoryWriteCount ?? 0,
                models);
        }).ToList();
    }
}

/// <summary>Confidence trend (Phase 1.5 §6) — raw points for the caller to chart,
/// since a single average can't show drift over time.</summary>
public sealed record ConfidencePointDto(DateTimeOffset At, double Confidence);

public sealed record GetAgentConfidenceTrendQuery(string AgentName, int Limit = 50)
    : IRequest<IReadOnlyCollection<ConfidencePointDto>>;

public sealed class GetAgentConfidenceTrendQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentConfidenceTrendQuery, IReadOnlyCollection<ConfidencePointDto>>
{
    public async Task<IReadOnlyCollection<ConfidencePointDto>> Handle(
        GetAgentConfidenceTrendQuery request, CancellationToken cancellationToken)
    {
        return await db.TaskNodes
            .Where(n => n.AssignedAgentName == request.AgentName && n.Confidence != null)
            .OrderByDescending(n => n.UpdatedAt)
            .Take(request.Limit)
            .Select(n => new ConfidencePointDto(n.UpdatedAt, n.Confidence!.Value))
            .ToListAsync(cancellationToken);
    }
}
