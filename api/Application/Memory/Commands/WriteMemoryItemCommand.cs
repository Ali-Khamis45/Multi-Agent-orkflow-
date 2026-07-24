using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Memory;
using MediatR;

namespace AiAgentsTeam.Application.Memory.Commands;

/// <summary>
/// Writes one memory item (ARCHITECTURE_EXTENSION.md §E5). Phase 1 wires up the
/// Working, Conversation, and Project layers; Workflow/LongTerm and embedding-based
/// retrieval are Phase 3 items (build order §24) — the schema already supports them.
/// </summary>
public sealed record WriteMemoryItemCommand(
    Guid WorkspaceId, MemoryLayer Layer, Guid ScopeRef, MemoryKind Kind, string Content,
    Guid? SourceArtifactId, DateTimeOffset? TtlAt) : IRequest<Guid>;

public sealed class WriteMemoryItemCommandHandler(IApplicationDbContext db)
    : IRequestHandler<WriteMemoryItemCommand, Guid>
{
    public async Task<Guid> Handle(WriteMemoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = new MemoryItem(
            request.WorkspaceId, request.Layer, request.ScopeRef, request.Kind, request.Content,
            request.SourceArtifactId, request.TtlAt);

        db.MemoryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }
}
