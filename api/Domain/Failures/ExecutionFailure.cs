using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Failures;

/// <summary>
/// Structured Error Model (Phase 1.5 §4) — replaces "a raw exception message in
/// ReasonJson" as the shape every task failure is recorded in. Distinct from
/// ReasoningTrace's lightweight per-stage ErrorMessage: this is the durable,
/// queryable record of *why a task failed*, rich enough to drive future
/// automated triage (Retryable/Recoverable) without re-parsing free text.
/// </summary>
public class ExecutionFailure : Entity
{
    public Guid TaskNodeId { get; private set; }
    public Guid WorkflowRunId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string Agent { get; private set; } = default!;

    public FailureCategory Category { get; private set; }
    public FailureSeverity Severity { get; private set; }
    public bool Recoverable { get; private set; }
    public bool Retryable { get; private set; }

    public string Message { get; private set; } = default!;
    public string? Stack { get; private set; }
    public string? SuggestedAction { get; private set; }

    private ExecutionFailure() { }

    public ExecutionFailure(
        Guid taskNodeId,
        Guid workflowRunId,
        Guid correlationId,
        string agent,
        FailureCategory category,
        FailureSeverity severity,
        bool recoverable,
        bool retryable,
        string message,
        string? stack,
        string? suggestedAction)
    {
        TaskNodeId = taskNodeId;
        WorkflowRunId = workflowRunId;
        CorrelationId = correlationId;
        Agent = agent;
        Category = category;
        Severity = severity;
        Recoverable = recoverable;
        Retryable = retryable;
        Message = message;
        Stack = stack;
        SuggestedAction = suggestedAction;
    }
}
