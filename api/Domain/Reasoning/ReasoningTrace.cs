using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Reasoning;

/// <summary>
/// The unified execution telemetry record (Phase 1.5 §1 Observability) — one row
/// per stage of one agent invocation's reasoning pipeline (ARCHITECTURE_EXTENSION.md
/// §E6). Deliberately flat and column-per-signal rather than a generic
/// attributes-bag, but every field maps onto an OpenTelemetry span concept
/// (WorkflowRunId/CorrelationId ~ trace id, TaskNodeId+Stage ~ span id,
/// StartedAt/DurationMs ~ span timing, everything else ~ span attributes) so a
/// future OTel exporter can translate this table directly into spans without
/// a schema redesign.
/// </summary>
public class ReasoningTrace : Entity
{
    public Guid TaskNodeId { get; private set; }
    public Guid WorkflowRunId { get; private set; }
    public Guid CorrelationId { get; private set; }

    public string Agent { get; private set; } = default!;
    public ReasoningStage Stage { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }
    public long DurationMs { get; private set; }

    public string? InputJson { get; private set; }
    public string? OutputJson { get; private set; }

    public int? Tokens { get; private set; }
    public double? Confidence { get; private set; }
    public string? ModelUsed { get; private set; }
    public int RetryCount { get; private set; }
    public int MemoryReads { get; private set; }
    public int MemoryWrites { get; private set; }
    public int ToolCalls { get; private set; }

    /// <summary>Placeholder estimate (ARCHITECTURE_EXTENSION.md §E7 Cost Efficiency) —
    /// no real per-provider pricing table yet; see ModelRouter for the heuristic.</summary>
    public double? CostEstimate { get; private set; }

    public string? ErrorMessage { get; private set; }

    private ReasoningTrace() { }

    public ReasoningTrace(
        Guid taskNodeId,
        Guid workflowRunId,
        Guid correlationId,
        string agent,
        ReasoningStage stage,
        DateTimeOffset startedAt,
        long durationMs,
        string? inputJson,
        string? outputJson,
        int? tokens,
        double? confidence,
        string? modelUsed,
        int retryCount,
        int memoryReads,
        int memoryWrites,
        int toolCalls,
        double? costEstimate,
        string? errorMessage)
    {
        TaskNodeId = taskNodeId;
        WorkflowRunId = workflowRunId;
        CorrelationId = correlationId;
        Agent = agent;
        Stage = stage;
        StartedAt = startedAt;
        DurationMs = durationMs;
        InputJson = inputJson;
        OutputJson = outputJson;
        Tokens = tokens;
        Confidence = confidence;
        ModelUsed = modelUsed;
        RetryCount = retryCount;
        MemoryReads = memoryReads;
        MemoryWrites = memoryWrites;
        ToolCalls = toolCalls;
        CostEstimate = costEstimate;
        ErrorMessage = errorMessage;
    }
}
