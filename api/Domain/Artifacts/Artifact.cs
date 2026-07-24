using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Artifacts;

/// <summary>
/// Any produced output (ARCHITECTURE.md §14.1). New versions of the same logical
/// artifact increment <see cref="Version"/> and link via <see cref="PreviousVersionId"/>
/// — nothing is overwritten, which is what makes the Code Diff Engine (§17.2) and
/// Rollback (§9.2) possible.
/// </summary>
public class Artifact : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? WorkflowRunId { get; private set; }
    public Guid? TaskNodeId { get; private set; }

    /// <summary>Threads one workflow execution across both runtimes and every
    /// record it produces (Phase 1.5 §2 Correlation IDs).</summary>
    public Guid? CorrelationId { get; private set; }

    /// <summary>
    /// Caller-supplied dedupe key (Phase 1.5 §3 Idempotency) — e.g. an agent
    /// derives it from "{taskNodeId}:{artifactName}" so a retried produce_artifact
    /// call returns the already-created artifact instead of a spurious new version.
    /// Unique per WorkflowRun when present (see ArtifactConfiguration).
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    public string Name { get; private set; } = default!;
    public ArtifactType Type { get; private set; }
    public string OwnerAgent { get; private set; } = default!;
    public int Version { get; private set; } = 1;
    public ArtifactStatus Status { get; private set; } = ArtifactStatus.Draft;

    /// <summary>Inline content for small text artifacts. Large/binary artifacts use StorageRef instead.</summary>
    public string? Content { get; private set; }
    public string? StorageRef { get; private set; }
    public Guid? PreviousVersionId { get; private set; }

    private Artifact() { }

    public Artifact(
        Guid workspaceId,
        string name,
        ArtifactType type,
        string ownerAgent,
        string? content,
        Guid? workflowRunId = null,
        Guid? taskNodeId = null,
        Guid? previousVersionId = null,
        int version = 1,
        Guid? correlationId = null,
        string? idempotencyKey = null)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Type = type;
        OwnerAgent = ownerAgent;
        Content = content;
        WorkflowRunId = workflowRunId;
        TaskNodeId = taskNodeId;
        PreviousVersionId = previousVersionId;
        Version = version;
        CorrelationId = correlationId;
        IdempotencyKey = idempotencyKey;
    }

    public Artifact CreateNewVersion(string ownerAgent, string? content, string? idempotencyKey = null)
    {
        Supersede();
        return new Artifact(
            WorkspaceId, Name, Type, ownerAgent, content, WorkflowRunId, TaskNodeId, Id, Version + 1,
            CorrelationId, idempotencyKey);
    }

    public void MarkFinal() => Status = ArtifactStatus.Final;

    public void Supersede() => Status = ArtifactStatus.Superseded;
}
