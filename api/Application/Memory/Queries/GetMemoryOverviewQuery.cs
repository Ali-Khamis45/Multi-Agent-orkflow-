using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Memory.Queries;

public sealed record MemoryLayerCountDto(string Layer, int Count);

public sealed record MemoryOverviewItemDto(
    Guid Id, string Layer, Guid ScopeRef, string Kind, string Content,
    Guid? SourceArtifactId, int Version, Guid? SupersededById, DateTimeOffset? TtlAt, DateTimeOffset CreatedAt);

public sealed record MemoryOverviewDto(
    IReadOnlyCollection<MemoryLayerCountDto> LayerCounts, IReadOnlyCollection<MemoryOverviewItemDto> Items);

/// <summary>
/// Workspace-wide memory browse for the Memory Inspector (ARCHITECTURE_EXTENSION.md
/// §E5) — unlike <see cref="QueryMemoryQuery"/> (which requires an exact scope), this
/// powers a dashboard-style overview across every scope in a workspace, optionally
/// narrowed to one layer.
/// </summary>
public sealed record GetMemoryOverviewQuery(Guid WorkspaceId, string? Layer = null, int Limit = 200)
    : IRequest<MemoryOverviewDto>;

public sealed class GetMemoryOverviewQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMemoryOverviewQuery, MemoryOverviewDto>
{
    public async Task<MemoryOverviewDto> Handle(GetMemoryOverviewQuery request, CancellationToken cancellationToken)
    {
        var layerCounts = await db.MemoryItems
            .Where(m => m.WorkspaceId == request.WorkspaceId)
            .GroupBy(m => m.Layer)
            .Select(g => new MemoryLayerCountDto(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        var query = db.MemoryItems.Where(m => m.WorkspaceId == request.WorkspaceId);
        if (!string.IsNullOrEmpty(request.Layer) && Enum.TryParse<Domain.Memory.MemoryLayer>(request.Layer, true, out var layer))
        {
            query = query.Where(m => m.Layer == layer);
        }

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(request.Limit)
            .Select(m => new MemoryOverviewItemDto(
                m.Id, m.Layer.ToString(), m.ScopeRef, m.Kind.ToString(), m.Content,
                m.SourceArtifactId, m.Version, m.SupersededById, m.TtlAt, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new MemoryOverviewDto(layerCounts, items);
    }
}
