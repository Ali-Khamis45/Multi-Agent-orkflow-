# Execution Flow

End-to-end path from a one-line goal to a completed workflow, as it actually runs — this is the
literal sequence behind the "Build a Task Management SaaS" demo.

## Sequence

```mermaid
sequenceDiagram
    actor U as User
    participant FE as Mission Control
    participant API as .NET API
    participant Redis as Redis Streams
    participant AI as AI Runtime
    participant DB as PostgreSQL

    U->>FE: "Build a Task Management SaaS"
    FE->>API: POST /api/intake
    API->>AI: POST /intake (proxy, server-to-server)
    AI->>API: POST /api/workflows/runs (create run)
    API->>DB: persist WorkflowRun
    AI->>API: POST /api/workflows/runs/{id}/nodes (BusinessAnalysis)
    API->>DB: persist TaskNode
    AI->>Redis: publish TaskDispatched
    API-->>FE: 200 { workflowRunId }
    FE->>API: join SignalR group workflow:{id}

    Redis->>AI: TaskDispatched (ai-runtime-agents group)
    AI->>AI: BusinessAnalyst runs 12-stage pipeline
    AI->>API: POST /api/artifacts (StructuredRequirements)
    AI->>API: POST /api/reasoning/traces (×12 stages)
    AI->>API: PUT task status = Completed
    API->>DB: persist artifact + traces + status
    API->>Redis: publish TaskCompleted

    Redis->>AI: TaskCompleted (orchestrator + supervisor logic)
    AI->>AI: Supervisor Brain expands DAG
    AI->>API: POST nodes: ProjectPlanning → ArchitectureDesign → {Backend, Frontend} → CodeReview → QA
    AI->>API: POST /api/supervisor/decisions (StrategySelection)
    API->>DB: persist new nodes/edges + decision

    Redis->>API: (via orchestrator group) new nodes ready
    API->>API: SchedulerService dispatches ready nodes
    API->>DB: persist Checkpoint (full DAG snapshot)
    API->>Redis: publish TaskDispatched (parallel: Backend + Frontend)

    Redis-->>API: signalr-relay group, every event
    API-->>FE: SignalR: workflowEvent (live)
    FE->>FE: invalidate queries, DAG re-renders

    Note over AI,API: repeats per node until QAValidation completes
    AI->>API: PUT run status = Completed
    API->>Redis: publish WorkflowRunCompleted
    API-->>FE: SignalR: workflowEvent
    FE->>U: DAG shows 7/7 complete, live
```

## What each stage actually persists

| Step | Written by | Where | Read later by |
|---|---|---|---|
| Workflow run created | AI runtime, via API | `WorkflowRuns` | Dashboard, Workflow Runs list |
| Task node created | AI runtime, via API | `TaskNodes` | Execution Graph |
| Reasoning stage completed | AI runtime, via API | `ReasoningTraces` | Reasoning Inspector, Telemetry Center |
| Artifact produced | AI runtime, via API | `Artifacts` (versioned) | Artifacts Explorer |
| Supervisor decision | AI runtime, via API | `SupervisorDecisions` | Supervisor Brain page |
| Scheduling pass snapshot | .NET API scheduler | `Checkpoints` | Execution Playback |
| Live event | .NET API (Redis relay) | not persisted — SignalR push only | Event Console, live DAG |

Every row above except the last is durable and queryable after the fact — the live SignalR event is
the *notification* that something changed; the dashboard always re-fetches the real row rather than
trusting the event payload as a source of truth.

## The DAG

The task graph isn't fixed up front — it starts as a single node (`BusinessAnalysis`) and the
Supervisor Brain expands it as each stage completes, based on what it learns. For the canonical demo
goal, the DAG that results looks like this:

```mermaid
flowchart LR
    BA["BusinessAnalysis<br/>business-analyst"] --> PP["ProjectPlanning<br/>project-manager"]
    PP --> AD["ArchitectureDesign<br/>system-architect"]
    AD --> BE["BackendImplementation<br/>backend-engineer"]
    AD --> FE["FrontendImplementation<br/>frontend-engineer"]
    BE --> CR["CodeReview<br/>code-reviewer"]
    FE --> CR
    CR --> QA["QAValidation<br/>qa-engineer"]

    classDef done fill:#14251c,stroke:#22c55e,color:#e2e8f0
    class BA,PP,AD,BE,FE,CR,QA done
```

`BackendImplementation` and `FrontendImplementation` share a column because they're dispatched
**together** — both become "ready" the instant `ArchitectureDesign` completes, since neither depends
on the other. This is what Mission Control's Execution Graph is drawn from directly: the frontend's
`lib/dag-layout.ts` assigns each node a column equal to its longest-path distance from a root, so
parallel branches visually align without any server-side layout hint.

`CodeReview` is a **join node** — it depends on both parallel branches. Because a node's inputs are
fixed at creation time but its parallel predecessors haven't necessarily finished yet, `CodeReview`
references its inputs *by artifact name*, not by ID, and resolves the latest version of each by name
once it actually starts (`GetLatestArtifactByNameQuery` — see [API Reference](../API.md)).

## Playback

Because the scheduler writes a `Checkpoint` (a complete node/edge/status snapshot) after **every**
scheduling pass, Mission Control's Execution Playback isn't an animation interpolating between "now"
and "the end" — it's a scrubber over real historical states. See
[Workflow Engine](WORKFLOW_ENGINE.md#checkpoints--execution-playback) for how checkpoints are built.
