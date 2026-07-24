using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Reasoning.Queries;

public sealed record ReasoningTraceDto(
    Guid Id, Guid WorkflowRunId, Guid CorrelationId, string Agent, string Stage,
    DateTimeOffset StartedAt, long DurationMs, string? InputJson, string? OutputJson,
    int? Tokens, double? Confidence, string? ModelUsed, int RetryCount,
    int MemoryReads, int MemoryWrites, int ToolCalls, double? CostEstimate,
    string? ErrorMessage, DateTimeOffset CreatedAt);

public sealed record GetReasoningTracesQuery(Guid TaskNodeId) : IRequest<IReadOnlyCollection<ReasoningTraceDto>>;

public sealed class GetReasoningTracesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReasoningTracesQuery, IReadOnlyCollection<ReasoningTraceDto>>
{
    public async Task<IReadOnlyCollection<ReasoningTraceDto>> Handle(GetReasoningTracesQuery request, CancellationToken cancellationToken)
    {
        return await db.ReasoningTraces
            .Where(t => t.TaskNodeId == request.TaskNodeId)
            .OrderBy(t => t.StartedAt)
            .Select(t => new ReasoningTraceDto(
                t.Id, t.WorkflowRunId, t.CorrelationId, t.Agent, t.Stage.ToString(),
                t.StartedAt, t.DurationMs, t.InputJson, t.OutputJson,
                t.Tokens, t.Confidence, t.ModelUsed, t.RetryCount,
                t.MemoryReads, t.MemoryWrites, t.ToolCalls, t.CostEstimate,
                t.ErrorMessage, t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
