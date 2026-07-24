using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Memory;

/// <summary>
/// One row in the single-table, five-layer memory model (ARCHITECTURE.md §13.1,
/// extended by ARCHITECTURE_EXTENSION.md §E5). Embedding is nullable in Phase 1 —
/// semantic retrieval is a Phase 3 item per the build order; scope/layer/kind are
/// wired up now so retrieval can be added without a schema change.
/// </summary>
public class MemoryItem : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public MemoryLayer Layer { get; private set; }
    public Guid ScopeRef { get; private set; }
    public MemoryKind Kind { get; private set; }
    public string Content { get; private set; } = default!;
    public Guid? SourceArtifactId { get; private set; }
    public double Score { get; private set; }
    public DateTimeOffset? TtlAt { get; private set; }
    public int Version { get; private set; } = 1;
    public Guid? SupersededById { get; private set; }

    private MemoryItem() { }

    public MemoryItem(
        Guid workspaceId,
        MemoryLayer layer,
        Guid scopeRef,
        MemoryKind kind,
        string content,
        Guid? sourceArtifactId = null,
        DateTimeOffset? ttlAt = null,
        Guid? correlationId = null)
    {
        WorkspaceId = workspaceId;
        Layer = layer;
        ScopeRef = scopeRef;
        Kind = kind;
        Content = content;
        SourceArtifactId = sourceArtifactId;
        TtlAt = ttlAt;
        CorrelationId = correlationId;
    }

    public void Touch(double score) => Score = score;

    public bool IsExpired() => TtlAt.HasValue && TtlAt.Value < DateTimeOffset.UtcNow;
}
