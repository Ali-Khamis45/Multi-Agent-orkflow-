# Memory

A single-table, five-layer memory model (`MemoryItems` in Postgres, owned entirely by the .NET API
— the AI runtime only ever reads/writes it through HTTP, via `ai-runtime/app/memory/memory_client.py`).

## The five layers

```mermaid
flowchart TD
    subgraph Layers["MemoryLayer"]
        W["Working<br/>per-task scratch space,<br/>cleared when the task completes"]
        C["Conversation<br/>intent-session Q&A<br/>and clarifications"]
        WF["Workflow<br/>facts shared across<br/>one run's tasks"]
        P["Project<br/>durable, cross-run<br/>workspace knowledge"]
        LT["LongTerm<br/>retained indefinitely<br/>across projects"]
    end

    W -.->|Phase 1 implemented| impl1[✓]
    C -.->|Phase 1 implemented| impl2[✓]
    P -.->|Phase 1 implemented| impl3[✓]
    WF -.->|modeled, not yet written| impl4[—]
    LT -.->|modeled, not yet written| impl5[—]

    classDef live fill:#14251c,stroke:#22c55e,color:#e2e8f0
    classDef planned fill:#1a1a1a,stroke:#6b7280,color:#9ca3af,stroke-dasharray: 4 4
    class W,C,P live
    class WF,LT planned
```

Phase 1 writes to **Working**, **Conversation**, and **Project**; **Workflow** and **LongTerm** are
fully modeled in the schema (so no migration is needed to activate them) but have no current writer
— see [Roadmap](../ROADMAP.md).

## Schema

Every `MemoryItem` carries:

| Field | Purpose |
|---|---|
| `WorkspaceId` | Which workspace this belongs to. |
| `Layer` | One of the five above. |
| `ScopeRef` | A GUID scoping the item — e.g. a `TaskNodeId` for Working memory, an `IntentSessionId` for Conversation. |
| `Kind` | `Requirement`, `Architecture`, `Code`, `Doc`, `Decision`, or `LessonLearned`. |
| `Content` | The actual text. |
| `SourceArtifactId` | Optional link back to the artifact this memory came from — this is the "Relationships" the Memory Inspector's "source artifact" link is built on. |
| `Score` | Reserved for embedding-similarity ranking — see below. |
| `Version` / `SupersededById` | Update history: a memory item can be superseded by a newer version rather than overwritten, so the Memory Inspector can show "v2, superseded" rather than silently losing the prior value. |
| `TtlAt` | Optional expiry, mainly relevant to Working memory. |

## Retrieval today vs. planned

Retrieval is currently **recency-ordered within (workspace, layer, scope)** —
`QueryMemoryQuery`/`GetMemoryOverviewQuery` order by `CreatedAt DESC`. The `Score` field exists
specifically so that embedding-similarity ranking (Vector Memory) can be added later as a *new
ordering strategy over the same schema*, not a redesign — this was a deliberate Phase 1 decision so
the retrieval interface wouldn't need to change shape when semantic search lands. See
[Roadmap](../ROADMAP.md).

## Where this surfaces in Mission Control

The **Memory Inspector** page shows: per-layer counts, a browsable feed across every scope (not
just one task), relationships to source artifacts, and version/supersession history. It also
displays two explicitly-labeled placeholder cards — **Knowledge Graph** and **Vector Memory** —
stating plainly that they're planned and not built, rather than rendering an empty or fabricated
panel.
