using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Workflow;

/// <summary>
/// One execution instance of a DAG (ARCHITECTURE.md §5.1). Aggregate root for
/// TaskNode/TaskEdge — the ready-set computation here is the exact algorithm
/// documented in §5.2 step 1: a node is ready when it is Pending and every
/// predecessor is Completed.
/// </summary>
public class WorkflowRun : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid? WorkflowDefinitionId { get; private set; }
    public string Goal { get; private set; } = default!;
    public WorkflowRunStatus Status { get; private set; } = WorkflowRunStatus.Planning;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private readonly List<TaskNode> _nodes = new();
    public IReadOnlyCollection<TaskNode> Nodes => _nodes.AsReadOnly();

    private readonly List<TaskEdge> _edges = new();
    public IReadOnlyCollection<TaskEdge> Edges => _edges.AsReadOnly();

    private WorkflowRun() { }

    public WorkflowRun(Guid workspaceId, string goal, Guid? workflowDefinitionId = null)
    {
        WorkspaceId = workspaceId;
        Goal = goal;
        WorkflowDefinitionId = workflowDefinitionId;
    }

    public TaskNode AddNode(string name, string taskType, string inputsJson, bool requiresApproval = false)
    {
        var node = new TaskNode(Id, name, taskType, inputsJson, requiresApproval);
        _nodes.Add(node);
        Touch();
        return node;
    }

    public TaskEdge AddDependency(TaskNode predecessor, TaskNode successor)
    {
        var edge = new TaskEdge(Id, predecessor.Id, successor.Id);
        _edges.Add(edge);
        Touch();
        return edge;
    }

    public void Start() => SetStatus(WorkflowRunStatus.Running);

    /// <summary>
    /// §5.2 step 1-2: nodes that are Pending and whose every predecessor is
    /// Completed. Approval-gated nodes (§9.1) are excluded until a human has
    /// resolved WaitingApproval, since they are not moved back to Pending here.
    /// </summary>
    public IReadOnlyCollection<TaskNode> ComputeReadySet()
    {
        var completedIds = _nodes
            .Where(n => n.Status == TaskNodeStatus.Completed)
            .Select(n => n.Id)
            .ToHashSet();

        return _nodes
            .Where(n => n.Status == TaskNodeStatus.Pending)
            .Where(n => _edges
                .Where(e => e.SuccessorNodeId == n.Id)
                .All(e => completedIds.Contains(e.PredecessorNodeId)))
            .ToList();
    }

    public bool IsComplete() => _nodes.Count > 0 && _nodes.All(n => n.Status == TaskNodeStatus.Completed);

    public bool HasUnrecoverableFailure() => _nodes.Any(n => n.Status == TaskNodeStatus.Failed);

    public void Complete() => SetStatus(WorkflowRunStatus.Completed);

    public void Fail() => SetStatus(WorkflowRunStatus.Failed);

    public void Pause() => SetStatus(WorkflowRunStatus.Paused);

    public void WaitForApproval() => SetStatus(WorkflowRunStatus.WaitingApproval);

    private void SetStatus(WorkflowRunStatus status)
    {
        Status = status;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
