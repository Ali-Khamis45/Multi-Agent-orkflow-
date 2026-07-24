using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Workflow;

/// <summary>
/// One DAG node (ARCHITECTURE.md §5.1), extended with Level/ParentNodeId for the
/// Hierarchical Task Planner (ARCHITECTURE_EXTENSION.md §E3). Phase 1 workflows use
/// flat Task-level nodes only (Level=Task, ParentNodeId=null) — the columns exist so
/// deeper hierarchies can be introduced later without a schema change.
/// </summary>
public class TaskNode : Entity
{
    public Guid WorkflowRunId { get; private set; }
    public TaskLevel Level { get; private set; } = TaskLevel.Task;
    public Guid? ParentNodeId { get; private set; }

    /// <summary>Inherited from the parent WorkflowRun at creation (Phase 1.5 §2).</summary>
    public Guid CorrelationId { get; private set; }

    public string Name { get; private set; } = default!;
    public string TaskType { get; private set; } = default!;
    public TaskNodeStatus Status { get; private set; } = TaskNodeStatus.Pending;

    public string? AssignedAgentName { get; private set; }
    public string? InputsJson { get; private set; }
    public string? OutputsJson { get; private set; }

    public double? Confidence { get; private set; }
    public string? RiskLevel { get; private set; }
    public string? ReasoningSummary { get; private set; }

    public bool RequiresApproval { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private TaskNode() { }

    public TaskNode(
        Guid workflowRunId,
        string name,
        string taskType,
        string inputsJson,
        Guid correlationId,
        bool requiresApproval = false,
        TaskLevel level = TaskLevel.Task,
        Guid? parentNodeId = null)
    {
        WorkflowRunId = workflowRunId;
        Name = name;
        TaskType = taskType;
        InputsJson = inputsJson;
        CorrelationId = correlationId;
        RequiresApproval = requiresApproval;
        Level = level;
        ParentNodeId = parentNodeId;
        Status = TaskNodeStatus.Pending;
    }

    public void MarkReady() => Transition(TaskNodeStatus.Ready);

    public void MarkWaitingApproval() => Transition(TaskNodeStatus.WaitingApproval);

    public void Dispatch(string agentName)
    {
        AssignedAgentName = agentName;
        AttemptCount++;
        Transition(TaskNodeStatus.Dispatched);
    }

    public void MarkRunning() => Transition(TaskNodeStatus.Running);

    public void Complete(string outputsJson, double confidence, string riskLevel, string? reasoningSummary)
    {
        OutputsJson = outputsJson;
        Confidence = confidence;
        RiskLevel = riskLevel;
        ReasoningSummary = reasoningSummary;
        Transition(TaskNodeStatus.Completed);
    }

    public void Fail(string? reasonJson)
    {
        OutputsJson = reasonJson;
        Transition(TaskNodeStatus.Failed);
    }

    public void Block() => Transition(TaskNodeStatus.Blocked);

    public void ResetForRetry()
    {
        AssignedAgentName = null;
        Transition(TaskNodeStatus.Pending);
    }

    private void Transition(TaskNodeStatus next)
    {
        Status = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
