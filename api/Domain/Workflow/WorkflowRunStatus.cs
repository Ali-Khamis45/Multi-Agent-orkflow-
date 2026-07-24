namespace AiAgentsTeam.Domain.Workflow;

public enum WorkflowRunStatus
{
    Planning,
    Running,
    WaitingApproval,
    Paused,
    Completed,
    Failed,
    RolledBack
}
