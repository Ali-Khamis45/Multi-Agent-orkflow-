# Architecture Overview

## System design

AI Agents Team is three cooperating services plus two data stores, each with one clear
responsibility:

| Service | Responsibility | Never does |
|---|---|---|
| **`api/`** (.NET 10) | Owns all durable state. Orchestrates workflow runs, schedules tasks, exposes the only HTTP/SignalR surface the frontend talks to. | Reason about *what* to do — it schedules and persists, it doesn't plan. |
| **`ai-runtime/`** (Python) | The "brain." Intent analysis, the 12-stage reasoning pipeline, the Supervisor Brain's dynamic DAG expansion, the multi-model router, 7 specialist agents. | Touch a database. Every write goes through the .NET API's HTTP endpoints. |
| **`frontend/`** (Next.js) | Mission Control — renders everything the platform does, live. | Talk to `ai-runtime` directly. Every request goes through the .NET API; live updates arrive over its SignalR hub. |
| **PostgreSQL** | System of record: workflow runs, task graphs, artifacts, memory, telemetry, checkpoints. | — |
| **Redis** | Event bus (Streams) + pub/sub for live updates. Not a system of record — nothing is only in Redis. | — |

Two boundaries are load-bearing and enforced, not just conventions:

1. **The AI runtime never touches Postgres.** Verified in the [Code Review](../reviews/CODE_REVIEW.md) —
   no `asyncpg`/`sqlalchemy`/`psycopg` import exists anywhere in `ai-runtime/app`. Every persistence
   operation is an HTTP call to the .NET API via `app/clients/api_client.py`.
2. **The frontend never talks to the AI runtime.** Verified the same way — no reference to the AI
   runtime's port or host exists anywhere in `frontend/src`. Two endpoints exist purely to proxy this
   server-to-server: `POST /api/intake` and `GET /api/prompts`.

## Overall system diagram

```mermaid
flowchart LR
    User(["Operator / Recruiter"])
    FE["Mission Control<br/>Next.js 16 / React 19"]
    API["ASP.NET Core API<br/>MediatR · EF Core · SignalR"]
    AI["AI Runtime<br/>FastAPI · Reasoning Pipeline · Supervisor Brain"]
    PG[(PostgreSQL)]
    Redis[(Redis<br/>Streams + PubSub)]
    LLM["Multi-Model Router<br/>Anthropic / OpenAI / Gemini / Ollama / Mock"]

    User -->|HTTPS| FE
    FE -->|REST + SignalR<br/>never direct| API
    API -->|EF Core| PG
    API -->|publish / consume| Redis
    API -->|HTTP, server-to-server<br/>proxy only| AI
    AI -->|HTTP| API
    AI -->|publish / consume| Redis
    AI --> LLM

    classDef svc fill:#1a2332,stroke:#3b82f6,color:#e2e8f0
    classDef store fill:#1a2332,stroke:#22c55e,color:#e2e8f0
    class FE,API,AI svc
    class PG,Redis store
```

## Why this shape

- **The API is the only writer to Postgres.** That means every durable fact about the system —
  what a task's status is, what an agent produced, what the Supervisor decided — has exactly one
  code path that can change it, which is what makes [Execution Playback](EXECUTION_FLOW.md#playback)
  possible: a `Checkpoint` row written after every scheduling pass is a complete, trustworthy
  snapshot, because nothing else could have mutated state out from under it.
- **Redis Streams, not a direct call, connects the API and the AI runtime for orchestration.** The
  API publishes `TaskDispatched`; the AI runtime's agents consume it, do the work, and publish
  `TaskCompleted`/`TaskFailed` back. Three independent consumer groups (`orchestrator`,
  `signalr-relay`, `ai-runtime-agents`) read the same stream, so a slow or crashed SignalR relay can
  never block scheduling, and a crashed agent process can never lose an event — see
  [Event Bus](EVENT_BUS.md).
- **The dashboard is a read/observe surface, not a second writer.** It calls the API for data and
  submits new work through `POST /api/intake`, but it never reaches into Postgres or Redis, and it
  never calls the AI runtime — every observable fact it renders (a DAG node's status, a reasoning
  trace, a supervisor decision) is a live projection of what the API layer already persisted.

## Folder structure

```
api/                              ASP.NET Core 10, Clean Architecture
  Domain/                         Entities, value objects, domain logic — zero dependencies
    Agents/ Artifacts/ Checkpoints/ Common/ Failures/ Intent/ Memory/ Reasoning/ Supervisor/ Workflow/ Workspaces/
  Application/                    CQRS (MediatR): one folder per feature, Commands/ + Queries/ inside
    Artifacts/ Checkpoints/ Intent/ Memory/ Observability/ Reasoning/ Registry/ Scheduling/ Supervisor/ Workflows/ Workspaces/
    Common/                       Shared interfaces (IApplicationDbContext, IEventBus, ...), pipeline behaviors
  Infrastructure/                 EF Core, Npgsql, Redis Streams, the AI-runtime HTTP client
  Api/                            Controllers, SignalR hubs, middleware, composition root (Program.cs)
  Tests/AiAgentsTeam.IntegrationTests/   Testcontainers-backed handler tests

ai-runtime/                       Python 3.12 / FastAPI, the system's "brain"
  app/
    agents/                       AgentBase + 7 concrete agents (declarative subclasses)
    clients/                      ApiClient (HTTP to .NET), RedisEventBus
    intent/                       Intent classification/complexity/ambiguity heuristics
    memory/                       MemoryClient (writes through the API, never direct)
    models/                       Wire-format models (EventEnvelope, etc.)
    orchestration/                Event consumers that dispatch work to agents
    reasoning/                    The 12-stage ReasoningPipeline + StructuredFailure
    routing/                      ModelRouter (multi-provider + deterministic mock fallback)
    supervisor/                   SupervisorAgent — dynamic DAG expansion
    tools/                        ToolRegistry, sandboxed FilesystemTool, PromptLoaderTool, PromptRegistry
    prompts/                      Versioned prompt JSON files (the Prompt Registry's source of truth)
  tests/                          Fake-based unit tests (no live network/DB)
  scripts/benchmark.py            Standalone performance-baseline CLI

frontend/                         Next.js 16 (App Router), React 19, TypeScript — Mission Control
  src/
    app/                          Routes: one folder per page (dashboard is app/page.tsx)
    components/                   Feature-organized (agents/ artifacts/ dashboard/ graph/ health/
                                   layout/ memory/ prompts/ supervisor/ telemetry/ workflows/),
                                   plus shared/ and ui/ (shadcn primitives)
    hooks/                        TanStack Query hooks — the only place components fetch data from
    lib/                          api-client.ts (the only network boundary), types.ts, pure helpers
    store/                        Zustand stores for client-only UI state

docs/                              Everything you're reading now
  architecture/                    This directory
  reviews/                         Code/Security/Performance review (Release 1.0)
  demo/                            Demo package (Release 1.0)
  screenshots/ video/              Mission Control tour assets
```

## Where to go next

- [Execution Flow](EXECUTION_FLOW.md) — what happens from "submit a goal" to "workflow complete," with the DAG diagram.
- [Reasoning Engine](REASONING_ENGINE.md) — the 12-stage pipeline every agent invocation runs through.
- [Supervisor Brain](SUPERVISOR_BRAIN.md) — how the DAG is built dynamically as work completes.
- [Agent Lifecycle](AGENT_LIFECYCLE.md) — registration, heartbeat, dispatch, execution, retirement.
- [Memory](MEMORY.md) — the five-layer memory model.
- [Event Bus](EVENT_BUS.md) — Redis Streams, consumer groups, event flow.
- [Workflow Engine](WORKFLOW_ENGINE.md) — the scheduler, checkpoints, idempotency.
- [API Reference](../API.md) — every endpoint, DTO, and error shape.
- [Deployment](../DEPLOYMENT.md) — running this for real, with a deployment diagram.
