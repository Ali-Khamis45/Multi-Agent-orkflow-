namespace AiAgentsTeam.Domain.Reasoning;

/// <summary>
/// The 12-stage Reasoning Engine pipeline (ARCHITECTURE_EXTENSION.md §E6), a strict
/// superset of the original 5-stage Reflection Loop (ARCHITECTURE.md §11.2): Plan,
/// Execute, Reflect+SelfCritique (=original Critique), Improve, Publish (=Final Answer).
/// </summary>
public enum ReasoningStage
{
    Observe,
    Understand,
    Think,
    Plan,
    RetrieveMemory,
    SelectTools,
    Execute,
    Reflect,
    SelfCritique,
    Improve,
    ConfidenceScore,
    Publish
}
