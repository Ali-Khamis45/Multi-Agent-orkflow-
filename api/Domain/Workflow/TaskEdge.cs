using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Workflow;

/// <summary>
/// A dependency edge (ARCHITECTURE.md §5.1): <see cref="SuccessorNodeId"/> cannot
/// become Ready until <see cref="PredecessorNodeId"/> is Completed (or Approved).
/// </summary>
public class TaskEdge : Entity
{
    public Guid WorkflowRunId { get; private set; }
    public Guid PredecessorNodeId { get; private set; }
    public Guid SuccessorNodeId { get; private set; }

    private TaskEdge() { }

    public TaskEdge(Guid workflowRunId, Guid predecessorNodeId, Guid successorNodeId)
    {
        if (predecessorNodeId == successorNodeId)
            throw new ArgumentException("A task cannot depend on itself.");

        WorkflowRunId = workflowRunId;
        PredecessorNodeId = predecessorNodeId;
        SuccessorNodeId = successorNodeId;
    }
}
