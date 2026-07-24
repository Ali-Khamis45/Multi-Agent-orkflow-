using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Supervisor;

/// <summary>
/// Audit trail of every executive decision the Supervisor Agent makes
/// (ARCHITECTURE_EXTENSION.md §E1). The mechanical DAG Scheduler (§5.2) is
/// unmodified — this table only records the advisory directives layered on top.
/// </summary>
public class SupervisorDecision : Entity
{
    public Guid WorkflowRunId { get; private set; }
    public SupervisorDecisionType DecisionType { get; private set; }
    public string InputSnapshotJson { get; private set; } = default!;
    public string Rationale { get; private set; } = default!;
    public double Confidence { get; private set; }
    public string? TargetNodeIdsJson { get; private set; }

    private SupervisorDecision() { }

    public SupervisorDecision(
        Guid workflowRunId,
        SupervisorDecisionType decisionType,
        string inputSnapshotJson,
        string rationale,
        double confidence,
        string? targetNodeIdsJson = null)
    {
        WorkflowRunId = workflowRunId;
        DecisionType = decisionType;
        InputSnapshotJson = inputSnapshotJson;
        Rationale = rationale;
        Confidence = confidence;
        TargetNodeIdsJson = targetNodeIdsJson;
    }
}
