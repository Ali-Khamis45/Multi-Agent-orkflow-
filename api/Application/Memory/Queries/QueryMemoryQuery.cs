using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Memory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Memory.Queries;

public sealed record MemoryItemDto(Guid Id, string Layer, Guid ScopeRef, string Kind, string Content, DateTimeOffset CreatedAt);

/// <summary>
/// Scoped memory lookup (ARCHITECTURE_EXTENSION.md §E5). Phase 1 retrieval is
/// recency-ordered within (workspace, layer, scope) — no embedding similarity yet
/// (Phase 3, per build order §24); the query shape already matches what semantic
/// retrieval will need, so adding vector ranking later is additive.
/// </summary>
public sealed record QueryMemoryQuery(Guid WorkspaceId, MemoryLayer Layer, Guid ScopeRef, int Limit = 20)
    : IRequest<IReadOnlyCollection<MemoryItemDto>>;

public sealed class QueryMemoryQueryHandler(IApplicationDbContext db)
    : IRequestHandler<QueryMemoryQuery, IReadOnlyCollection<MemoryItemDto>>
{
    public async Task<IReadOnlyCollection<MemoryItemDto>> Handle(QueryMemoryQuery request, CancellationToken cancellationToken)
    {
        return await db.MemoryItems
            .Where(m => m.WorkspaceId == request.WorkspaceId && m.Layer == request.Layer && m.ScopeRef == request.ScopeRef)
            .OrderByDescending(m => m.CreatedAt)
            .Take(request.Limit)
            .Select(m => new MemoryItemDto(m.Id, m.Layer.ToString(), m.ScopeRef, m.Kind.ToString(), m.Content, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
