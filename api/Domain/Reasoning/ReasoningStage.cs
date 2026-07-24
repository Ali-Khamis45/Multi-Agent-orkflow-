namespace AiAgentsTeam.Domain.Reasoning;

/// <summary>
/// The 12-stage Reasoning Engine pipeline (ARCHITECTURE_EXTENSION.md §E6), a strict
/// superset of the original 5-stage Reflection Loop (ARCHITECTURE.md §11.2): Plan,
/// Execute, Reflect+SelfCritique (=original Critique), Publish (=Final Answer).
///
/// RetrieveContext was split out from RetrieveMemory during Phase 1 implementation
/// (Milestone 2) to distinguish "assemble this task's working context" (inputs,
/// upstream artifacts) from "query the layered Memory store" (E5) — both still
/// precede tool selection. Stored as a string column (see ReasoningTraceConfiguration),
/// so this reshaping required no migration.
/// </summary>
public enum ReasoningStage
{
    Observe,
    Understand,
    Think,
    Plan,
    RetrieveContext,
    RetrieveMemory,
    SelectTools,
    Execute,
    Reflect,
    SelfCritique,
    ConfidenceEvaluation,
    PublishResult
}
