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
    Agents/ Artifacts/ Checkpoints/ Common/ Failures/ Intent/ Memory/ Reasoning/ Supervisor/ Users/ Workflow/ Workspaces/
                                   (Users/ holds User + CompanyType — Phase 2, see below)
  Application/                    CQRS (MediatR): one folder per feature, Commands/ + Queries/ inside
    Artifacts/ Checkpoints/ Intent/ Memory/ Observability/ Reasoning/ Registry/ Scheduling/ Supervisor/ Workflows/ Workspaces/
    Common/                       Shared interfaces (IApplicationDbContext, IEventBus, ...), pipeline behaviors
  Infrastructure/                 EF Core, Npgsql, Redis Streams, the AI-runtime HTTP client
  Api/                            Controllers, SignalR hubs, middleware, composition root (Program.cs)
  Tests/AiAgentsTeam.IntegrationTests/   Testcontainers-backed handler tests

ai-runtime/                       Python 3.12 / FastAPI, the system's "brain"
  app/
    agents/                       AgentBase + 7 Software agents + 11 founder-* agents (Phase 2)
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
                                   login/ register/ — Phase 2 auth pages
                                   founder/ — Phase 2 Founder Workspace (own layout + 14 pages)
    components/                   Feature-organized (agents/ artifacts/ auth/ dashboard/ founder/
                                   graph/ health/ layout/ memory/ prompts/ supervisor/ telemetry/
                                   workflows/), plus shared/ and ui/ (shadcn primitives)
    hooks/                        TanStack Query hooks — the only place components fetch data from
    lib/                          api-client.ts (the only network boundary — attaches the JWT),
                                   types.ts, pure helpers
    store/                        Zustand stores for client-only UI state, incl. auth-store.ts
                                   (JWT + current user, Phase 2) and workspace-store.ts

docs/                              Everything you're reading now
  architecture/                    This directory
  reviews/                         Code/Security/Performance review (Release 1.0)
  demo/                            Demo package (Release 1.0)
  screenshots/ video/              Mission Control tour assets
```

## Phase 2 — AI Enterprise OS (Workspaces)

Release 1.0 shipped one product: an autonomous AI software engineering company. Phase 2 turns the
same shared infrastructure into a platform with multiple **Workspaces** — a Workspace is a
specialized AI company (its own agents, its own fixed pipeline, its own frontend shell) built on
top of the infrastructure above, which stays exactly as documented in every section above it.
Nothing in §"System design" changed: still three services, still the same two enforced boundaries.

Two Workspaces exist today: **Software Company** (Release 1.0, unchanged) and **Founder Workspace**
(new — a business operating system for startup founders, with 11 specialist agents: CEO, Business
Analyst, Market/Customer Researcher, Brand Strategist, Financial Advisor, Marketing Director,
Operations Manager, Sales Strategist, Growth Strategist, Legal Advisor).

**A user belongs to exactly one Workspace, chosen at registration and permanent.** This is modeled
as a `CompanyType` (`SoftwareCompany` | `Founder`) on the new `User` entity — deliberately *not* a
new meaning for the pre-existing `Workspace` entity (a named project container, many-per-user,
unrelated to product routing; see `Domain/Workspaces/`). A `User` owns one or more `Workspace`s; a
`User` has exactly one `CompanyType`.

```mermaid
flowchart TB
    Reg["POST /api/auth/register<br/>{ email, password, name, companyType }"] --> JWT["JWT<br/>company_type claim"]
    JWT --> Intake["POST /api/intake (Authorize)<br/>companyType read from JWT, never the body"]
    Intake --> Branch{"CompanyType"}
    Branch -->|SoftwareCompany| SWPipe["7-node Software pipeline<br/>(unchanged from Release 1.0)"]
    Branch -->|Founder| FPipe["11-node Founder pipeline<br/>CEO → BizModel → {Market,Customer}<br/>→ Brand → {Fin,Mktg,Ops,Sales} → Growth → Legal"]
    SWPipe --> Sched["DAG Scheduler §5.2 (unchanged)"]
    FPipe --> Sched
```

What this added, concretely:
- **Auth**: JWT bearer (`api/auth/register|login|me`) — see [API Reference](../API.md#auth--apiauth).
- **"Master Supervisor" is JWT-derived routing, not a new classifier.** Because `CompanyType` is
  fixed per account, the spec's "classify which company a request belongs to" responsibility is
  already resolved by authentication: `IntakeController` reads `company_type` off the caller's own
  token. No separate cross-company intent-classification service was built — it would have
  duplicated what the JWT already guarantees.
- **CompanyType-scoped Agent Registry**: `AgentRegistration` is now unique on `(Name, CompanyType)`,
  not `Name` alone, so both companies can register an agent with the same short role name. The
  Python-side agent dict avoids this entirely by prefixing every Founder agent's name with
  `founder-`.
- **Supervisor Brain gained a second fixed pipeline** (`app/supervisor/supervisor_agent.py`),
  selected by the `companyType` threaded from `kickoff()` through to DAG expansion — the Software
  pipeline's code path is untouched.
- **Frontend**: a route guard (`components/auth/auth-gate.tsx`) enforces "Software users cannot
  access Founder pages, Founder users cannot access Software pages" for every route, present and
  future, without each page needing its own check. The Founder Workspace gets its own shell
  (`app/founder/`) — sidebar, dashboard, and 14 pages — rather than reusing Mission Control's
  Software-flavored chrome.

**Known gap, tracked rather than hidden:** the .NET Scheduler still matches ready tasks to agents by
`TaskType` alone, with no `CompanyType` filter — safe today only because every Founder task type
was deliberately named distinctly from every Software task type. A future hardening pass should add
the same `CompanyType` scoping the Registry already has.

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
