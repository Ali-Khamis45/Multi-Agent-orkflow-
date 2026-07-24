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

/// <summary>
/// Resolves the latest version of a named artifact within a WorkflowRun. Exists
/// because a DAG node's InputsJson is fixed at node-creation time (before its
/// predecessors have run), so a join node (e.g. Code Reviewer, depending on both
/// parallel Backend and Frontend outputs) cannot reference an exact ArtifactId in
/// advance — it references the artifact by name instead, and the AI Runtime's
/// RetrieveContext stage (§E6) resolves it via this query once the predecessor
/// has actually produced it.
/// </summary>
public sealed record GetLatestArtifactByNameQuery(Guid WorkflowRunId, string Name) : IRequest<ArtifactDto?>;

public sealed class GetLatestArtifactByNameQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetLatestArtifactByNameQuery, ArtifactDto?>
{
    public async Task<ArtifactDto?> Handle(GetLatestArtifactByNameQuery request, CancellationToken cancellationToken)
    {
        var latest = await db.Artifacts
            .Where(a => a.WorkflowRunId == request.WorkflowRunId && a.Name == request.Name)
            .OrderByDescending(a => a.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null ? null : GetArtifactQueryHandler.ToDto(latest);
    }
}
