namespace AiAgentsTeam.Domain.Workflow;

/// <summary>
/// Hierarchical Task Planner levels (ARCHITECTURE_EXTENSION.md §E3). Only leaf-level
/// nodes (Task/SubTask with no children) are ever dispatched by the Scheduler;
/// container levels (Goal..UserStory) have their status derived by roll-up.
/// </summary>
public enum TaskLevel
{
    Goal,
    Epic,
    Feature,
    UserStory,
    Task,
    SubTask
}
