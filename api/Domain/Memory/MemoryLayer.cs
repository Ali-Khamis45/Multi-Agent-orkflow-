namespace AiAgentsTeam.Domain.Memory;

/// <summary>Multi-Layer Memory (ARCHITECTURE_EXTENSION.md §E5). Phase 1 implements
/// Working, Conversation, and Project; Workflow and LongTerm are modeled here so no
/// interface changes are needed when they're activated in a later phase.</summary>
public enum MemoryLayer
{
    Working,
    Conversation,
    Workflow,
    Project,
    LongTerm
}
