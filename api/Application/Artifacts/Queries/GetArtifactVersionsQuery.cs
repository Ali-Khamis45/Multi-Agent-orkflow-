using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Artifacts.Queries;

public sealed record ArtifactDto(
    Guid Id, string Name, string Type, string OwnerAgent, int Version, string Status,
    string? Content, Guid? PreviousVersionId, DateTimeOffset CreatedAt);

public sealed record GetArtifactQuery(Guid ArtifactId) : IRequest<ArtifactDto?>;

public sealed class GetArtifactQueryHandler(IApplicationDbContext db) : IRequestHandler<GetArtifactQuery, ArtifactDto?>
{
    public async Task<ArtifactDto?> Handle(GetArtifactQuery request, CancellationToken cancellationToken)
    {
        var a = await db.Artifacts.FirstOrDefaultAsync(x => x.Id == request.ArtifactId, cancellationToken);
        return a is null ? null : ToDto(a);
    }

    internal static ArtifactDto ToDto(Domain.Artifacts.Artifact a) => new(
        a.Id, a.Name, a.Type.ToString(), a.OwnerAgent, a.Version, a.Status.ToString(),
        a.Content, a.PreviousVersionId, a.CreatedAt);
}

/// <summary>All versions of a logical artifact, newest first — walks the PreviousVersionId chain.</summary>
public sealed record GetArtifactVersionsQuery(Guid ArtifactId) : IRequest<IReadOnlyCollection<ArtifactDto>>;

public sealed class GetArtifactVersionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetArtifactVersionsQuery, IReadOnlyCollection<ArtifactDto>>
{
    public async Task<IReadOnlyCollection<ArtifactDto>> Handle(GetArtifactVersionsQuery request, CancellationToken cancellationToken)
    {
        var all = await db.Artifacts.ToListAsync(cancellationToken);
        var byPrevious = all.Where(a => a.PreviousVersionId.HasValue).ToDictionary(a => a.PreviousVersionId!.Value);
        var byId = all.ToDictionary(a => a.Id);

        var current = all.FirstOrDefault(a => a.Id == request.ArtifactId);
        if (current is null) return [];

        // Walk backward to the first version.
        while (current.PreviousVersionId is { } prevId && byId.TryGetValue(prevId, out var prev))
            current = prev;

        var chain = new List<Domain.Artifacts.Artifact>();
        var cursor = current;
        while (true)
        {
            chain.Add(cursor);
            if (!byPrevious.TryGetValue(cursor.Id, out var next)) break;
            cursor = next;
        }

        chain.Reverse();
        return chain.Select(GetArtifactQueryHandler.ToDto).ToList();
    }
}
