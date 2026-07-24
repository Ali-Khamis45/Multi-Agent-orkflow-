using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Checkpoints;

/// <summary>
/// A durable snapshot of WorkflowRun + TaskNode state at a milestone
/// (ARCHITECTURE.md §9.2). Phase 1 only writes checkpoints for resume/audit;
/// rollback/fork/replay execution is deferred to Phase 2 per the build order.
/// </summary>
public class Checkpoint : Entity
{
    public Guid WorkflowRunId { get; private set; }
    public string Label { get; private set; } = default!;
    public string SnapshotJson { get; private set; } = default!;

    private Checkpoint() { }

    public Checkpoint(Guid workflowRunId, string label, string snapshotJson)
    {
        WorkflowRunId = workflowRunId;
        Label = label;
        SnapshotJson = snapshotJson;
    }
}
