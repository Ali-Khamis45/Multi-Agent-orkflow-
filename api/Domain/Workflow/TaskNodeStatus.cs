namespace AiAgentsTeam.Domain.Workflow;

public enum TaskNodeStatus
{
    Pending,
    Ready,
    Dispatched,
    Running,
    Completed,
    Failed,
    Blocked,
    WaitingApproval
}
