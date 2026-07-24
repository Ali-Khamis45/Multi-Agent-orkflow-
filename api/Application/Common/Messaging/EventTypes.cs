namespace AiAgentsTeam.Application.Common.Messaging;

/// <summary>
/// The core event catalog (ARCHITECTURE.md §6.3), restricted to the Phase 1 subset
/// actually published by this build. New event types are additive string values —
/// no central enum change is required for a plugin to introduce its own.
/// </summary>
public static class EventTypes
{
    public const string TaskCreated = nameof(TaskCreated);
    public const string TaskDispatched = nameof(TaskDispatched);
    public const string TaskCompleted = nameof(TaskCompleted);
    public const string TaskFailed = nameof(TaskFailed);
    public const string WorkflowRunStarted = nameof(WorkflowRunStarted);
    public const string WorkflowRunCompleted = nameof(WorkflowRunCompleted);
    public const string WorkflowRunFailed = nameof(WorkflowRunFailed);
    public const string AgentRegistered = nameof(AgentRegistered);
    public const string AgentUnavailable = nameof(AgentUnavailable);

    // E1 Supervisor Brain
    public const string SupervisorDirectiveIssued = nameof(SupervisorDirectiveIssued);
    public const string SupervisorReplanRequested = nameof(SupervisorReplanRequested);
    public const string SupervisorStrategySelected = nameof(SupervisorStrategySelected);

    // E2 Intent Engine
    public const string IntentAnalysisStarted = nameof(IntentAnalysisStarted);
    public const string ClarificationRequested = nameof(ClarificationRequested);
    public const string ClarificationAnswered = nameof(ClarificationAnswered);
    public const string RequirementsStructured = nameof(RequirementsStructured);
    public const string ProjectClassified = nameof(ProjectClassified);

    // E6 Reasoning Engine
    public const string ReasoningStageCompleted = nameof(ReasoningStageCompleted);
}
