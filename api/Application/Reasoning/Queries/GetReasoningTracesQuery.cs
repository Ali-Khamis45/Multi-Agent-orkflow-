using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Reasoning.Queries;

public sealed record ReasoningTraceDto(Guid Id, string Agent, string Stage, string? InputJson, string? OutputJson, long DurationMs, DateTimeOffset CreatedAt);

public sealed record GetReasoningTracesQuery(Guid TaskNodeId) : IRequest<IReadOnlyCollection<ReasoningTraceDto>>;

public sealed class GetReasoningTracesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetReasoningTracesQuery, IReadOnlyCollection<ReasoningTraceDto>>
{
    public async Task<IReadOnlyCollection<ReasoningTraceDto>> Handle(GetReasoningTracesQuery request, CancellationToken cancellationToken)
    {
        return await db.ReasoningTraces
            .Where(t => t.TaskNodeId == request.TaskNodeId)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new ReasoningTraceDto(t.Id, t.Agent, t.Stage.ToString(), t.InputJson, t.OutputJson, t.DurationMs, t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
