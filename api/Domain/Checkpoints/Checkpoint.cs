using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Checkpoints;

/// <summary>
/// A durable, lightweight snapshot of WorkflowRun + TaskNode state
/// (ARCHITECTURE.md §9.2; Phase 1.5 §5 Execution Snapshots). Written after every
/// scheduling pass so Resume/Replay/Checkpoint/Debugging (§9.2 full semantics)
/// can be implemented later purely by reading this table — no storage redesign.
/// </summary>
public class Checkpoint : Entity
{
    public Guid WorkflowRunId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string Label { get; private set; } = default!;
    public string SnapshotJson { get; private set; } = default!;

    private Checkpoint() { }

    public Checkpoint(Guid workflowRunId, Guid correlationId, string label, string snapshotJson)
    {
        WorkflowRunId = workflowRunId;
        CorrelationId = correlationId;
        Label = label;
        SnapshotJson = snapshotJson;
    }
}
