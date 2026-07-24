using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Reasoning;

/// <summary>
/// One stage of one agent invocation's reasoning pipeline (ARCHITECTURE_EXTENSION.md
/// §E6). Written by the AI Runtime's Execution Engine for every agent, every stage —
/// this is what makes the Reasoning Steps observability panel (§E11) possible.
/// </summary>
public class ReasoningTrace : Entity
{
    public Guid TaskNodeId { get; private set; }
    public string Agent { get; private set; } = default!;
    public ReasoningStage Stage { get; private set; }
    public string? InputJson { get; private set; }
    public string? OutputJson { get; private set; }
    public long DurationMs { get; private set; }

    private ReasoningTrace() { }

    public ReasoningTrace(Guid taskNodeId, string agent, ReasoningStage stage, string? inputJson, string? outputJson, long durationMs)
    {
        TaskNodeId = taskNodeId;
        Agent = agent;
        Stage = stage;
        InputJson = inputJson;
        OutputJson = outputJson;
        DurationMs = durationMs;
    }
}
