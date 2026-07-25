# Code Review — Release 1.0

Scope: `api/` (.NET 10 Clean Architecture), `ai-runtime/` (Python/FastAPI), `frontend/` (Next.js 16/React 19).
Method: full manual review of dependency direction, layering, error handling, logging, async
correctness, dead code, duplication, and naming across all three services, cross-checked against
the actual `.csproj`/`pyproject.toml`/`package.json` dependency graphs and a live `dotnet build` /
`tsc --noEmit`.

**Overall assessment**: all three codebases are unusually disciplined for a pre-1.0 project —
dependency direction is correct everywhere it was checked, naming is consistent, there is
essentially no dead-code sprawl, and duplication is minor. The real gaps cluster around
**observability and error handling on the .NET side**, and **test coverage on the Python and
frontend sides**. None of the findings below block a 1.0 release; they're the punch list for the
first hardening pass after it.

---

## 1. `api/` — ASP.NET Core

### Dependency direction & layering — clean
Verified via all four `.csproj` files:

- `Domain` — zero `ProjectReference`s, zero framework packages.
- `Application` — references only `Domain` (plus MediatR/FluentValidation/EF Core *abstractions* —
  see note below).
- `Infrastructure` — references `Application` + `Domain`, never `Api`.
- `Api` — references all three.

No violation found. Every controller is thin (deserialize → `sender.Send(...)` → map to
`ActionResult`); no controller touches `DbContext` or Infrastructure directly.

**Accepted leak**: `IApplicationDbContext` (`Application/Common/Interfaces/IApplicationDbContext.cs`)
exposes `DbSet<T>` directly, so `Application` is aware of EF Core's `IQueryable` semantics, not a
persistence-agnostic repository contract. Standard in Clean Architecture templates and not worth
unwinding for 1.0, but it means swapping the ORM later touches every query handler, not just
`Infrastructure`.

### High-severity: no global exception-handling middleware
`Program.cs` registers only `ValidationExceptionMiddleware`, which narrowly catches
`FluentValidation.ValidationException`. Eleven call sites throw `KeyNotFoundException` for a
missing aggregate (`StartWorkflowRunCommand`, `CompleteTaskCommand`, `FailTaskCommand`,
`AddTaskNodeCommand`, `AddTaskDependencyCommand`, `RecordIntentAnalysisCommand`,
`SubmitClarificationAnswerCommand`, `MarkIntentStructuredCommand`, `RecordSupervisorDecisionCommand`,
`CreateArtifactCommand`, `SchedulerService`) and none of them are caught — they surface as bare 500s
with a stack trace, inconsistent with the controllers that check for `null` and return a proper
`NotFound()` (e.g. `WorkflowsController`, `ArtifactsController`). **Fix**: add an `IExceptionHandler`
(or `UseExceptionHandler` + `ProblemDetails`) that maps `KeyNotFoundException` → 404 and everything
else → structured 500, consistent with the `ValidationExceptionMiddleware` shape already in place.

### High-severity: logging coverage is far below the handler count
`ILogger` is used in exactly 4 files (`CorrelationIdMiddleware`, `SchedulerService`,
`RedisStreamsEventBus`, `OrchestratorEventConsumer`) out of roughly 30 command/query handlers. No
controller and no handler logs anything, including on the `KeyNotFoundException` paths above.
`CorrelationIdMiddleware` correctly pushes `CorrelationId` into `logger.BeginScope`, but since
almost nothing downstream logs, the correlation-ID-to-log-line story the class's own doc comment
promises isn't realized in practice. **Fix**: at minimum, log at the point every unhandled
exception is caught by the new global handler above, with the correlation ID already in scope.

### Medium: two duplicated query blocks
`CompleteTaskCommand` and `FailTaskCommand` both independently do "resolve `WorkflowRunId` from a
`TaskNodeId`, then load the full `WorkflowRun` with `Nodes`+`Edges` or throw
`KeyNotFoundException`" — worth extracting into one shared helper (e.g. on `ISchedulerService`).

### Medium: `OrchestratorEventConsumer` routes on a `switch`
Adding a new reactive event type means editing `OrchestratorEventConsumer.HandleAsync`'s `switch`
statement (OCP violation, though a mild one at this scale). A dictionary of
`string -> Func<EventEnvelope, Task>`, or per-type `INotificationHandler`s, would let new event
types register without touching this class.

### Low
- Validator coverage is thin: **7 of 30** `IRequest` commands/queries have a matching
  `AbstractValidator`. The `ValidationBehavior` pipeline is correctly wired for the ones that do;
  the other 23 simply pass through unchecked into handlers (see also Security Review §Input
  Validation).
- `IntakeController`/`PromptsController` skip MediatR/`ISender` entirely, calling `IAiRuntimeClient`
  directly — still DIP-compliant (the interface lives in `Application`), but it's a second,
  unexplained request-handling convention living alongside CQRS-everywhere-else.
- Dead domain methods, never called: `WorkflowRun.Pause()`, `WorkflowRun.WaitForApproval()`,
  `WorkflowRun.HasUnrecoverableFailure()`, `TaskNode.MarkReady()`, `MarkWaitingApproval()`,
  `Block()`, `MarkRunning()` — scaffolding for the approval-gating feature, not yet wired up.
- `WorkflowDefinition` has a `DbSet`, EF config, and migration but is never read or written by any
  handler — an entirely inert table.
- A handful of unused `EventTypes` constants (`TaskCreated`, `AgentUnavailable`,
  `SupervisorReplanRequested`, `SupervisorStrategySelected`, `ReasoningStageCompleted`).
- No `async void`, no `.Result`/`.Wait()` blocking calls, async naming is consistently `Async`-suffixed.

### Test coverage
One integration-test project (`Tests/AiAgentsTeam.IntegrationTests`), 14 `[Fact]`s against real
Postgres/Redis via Testcontainers, exercising handlers directly through `ISender`. **No HTTP-layer
tests** (`WebApplicationFactory`) exist at all — controllers, middleware, JSON enum conversion,
CORS, and the SignalR hub are entirely untested. Untested at the handler level: list/filter queries
added in Phase 1.6 (`GetWorkflowRunsQuery`, `GetSupervisorSummaryQuery`, `GetMemoryOverviewQuery`,
`GetReasoningTelemetryQuery`, `GetAgentReasoningTracesQuery`, `GetCheckpointsQuery`), the entire
Observability feature, and every FluentValidation validator (no negative-path tests).

---

## 2. `ai-runtime/` — Python/FastAPI

This is the cleanest of the three services. Highlights:

- **No direct database access anywhere** (grepped for `asyncpg`, `sqlalchemy`, `psycopg`,
  `sqlite3`, `create_engine` — zero hits), confirming the architectural rule that this service only
  persists through the .NET API's HTTP endpoints actually holds, not just in comments.
- **No circular imports**; strictly layered (`agents/` → `reasoning/`, `clients/`, `tools/`,
  `routing/`, `memory/`; nothing lower-level imports back upward).
- The 7 agent implementations are genuinely DRY — each is a ~20-line declarative subclass of
  `AgentBase`, none reimplement pipeline/retry/telemetry/persistence logic.
- `StructuredFailure`/`classify_exception` (`app/reasoning/failures.py`) is a single, well-tested
  failure-classification boundary used uniformly — no agent hand-rolls its own error mapping.
- Consistent structured JSON logging via `app/logging_config.py`; zero stray `print()` in `app/`.
- No bare `except:` / `except Exception: pass` anywhere.

### Medium: blocking file I/O on the hot path
`PromptRegistry.render` does a synchronous `Path.read_text()` with no `asyncio.to_thread` offload,
called from `PromptLoaderTool.execute` (async) on **every** agent invocation via
`AgentBase.generate`. Files are small so the stall is brief today, but it blocks the single event
loop under concurrent load — and this system explicitly relies on parallel dispatch (e.g. Backend +
Frontend running together). Worth an `asyncio.to_thread` wrap before 1.0 if concurrency increases.

### Medium: one payload-parsing gap can escape the structured-failure boundary
`AgentBase.handle_task_dispatched` does `json.loads(envelope.payload_json)` and dict-indexes
`payload["TaskType"]`/`payload["Name"]` **before** the `try:` block that would otherwise route
failures through `StructuredFailure`. A malformed/legacy payload raises `KeyError`/
`JSONDecodeError` that propagates into the fire-and-forget task in `event_consumer.py` instead of
producing a `TaskFailed` event — and since that task's `add_done_callback` only discards the task
(no error logging), the failure would surface only as asyncio's default "exception was never
retrieved" warning.

### Low
- `AgentBase.retry()` is defined but never called by any agent or the pipeline.
- `MemoryClient.remember_conversation`/`recall_conversation` and the `Workflow`/`LongTerm` memory
  layers are modeled but unused — expected, since Phase 1 only implements
  Working/Conversation/Project.
- Most `EventTypes` constants have no producer or consumer yet (reserved surface for later phases).
- No `Protocol`/`ABC` seams for `ApiClient`/`MemoryClient`/`ToolRegistry` — substitutability works
  today only via Python duck-typing in tests, not an enforced contract.
- Minor repeated HTTP-call and JSON-serialization idioms in `ModelRouter` and `ApiClient` (each
  provider/endpoint method repeats `post → raise_for_status → parse`); a thin helper would remove
  ~40 lines of repetition, not a correctness issue.

### Test coverage
560 lines across 6 files, all fake-based (no live network/DB), well-targeted: the full reasoning
pipeline (all 12 stages), `StructuredFailure`, the model router's fallback logic, the prompt
registry, and the sandbox's path/size defenses. **Zero coverage** on all 7 concrete agent classes,
`SupervisorAgent`, `IntentEngine`, `ApiClient`, `RedisEventBus`, `MemoryClient`, the event consumers,
and `app/main.py`'s FastAPI endpoints (no `TestClient` usage anywhere).

---

## 3. `frontend/` — Next.js Mission Control

Also clean. Highlights:

- Zero `any`, zero `@ts-ignore`/`@ts-expect-error` anywhere in `src`.
- Consistent kebab-case files / PascalCase components / camelCase hooks throughout.
- No prop-drilling — cross-cutting state lives in Zustand, read via per-component selectors.
- Chart theming is properly centralized (`components/telemetry/chart-card.tsx`) and reused, not
  copy-pasted, between the Telemetry Center and Supervisor Brain pages.
- **Python boundary confirmed held**: no reference to port 8000 or an AI-runtime host anywhere in
  `src`; `lib/api-client.ts` and `lib/signalr.ts` both point exclusively at the .NET API.

### High: no error boundary, no test coverage
No React error boundary and no Next.js `error.tsx`/`global-error.tsx` anywhere — a render-time
exception crashes to the framework's default overlay with no graceful fallback. Separately,
**there is no test suite at all**: no `.test.ts(x)` files, no test framework in `package.json`
(`jest`/`vitest`/`@testing-library/*`/`playwright`/`cypress` are all absent — only `eslint`).
Untested logic worth flagging specifically: `lib/health-score.ts`, `lib/dag-layout.ts`, and
`lib/export.ts`, none of which are trivial.

### Medium: query-level errors are invisible to the user
All 3 `useMutation` call sites (intake form, demo CTA, command-palette replay) correctly show a
toast on error. But **TanStack Query reads have no error handling anywhere** — only 3 of roughly 20
data-fetching components even destructure `isError`. When a fetch fails, `isLoading` becomes
`false` and `data` stays `undefined`, so the UI renders its normal empty state ("No agents match
these filters") indistinguishably from a real empty result. There's no global `onError`/logging
hook on the `QueryClient` either.

### Low
- `components/artifacts/artifact-versions.tsx` exports `ArtifactTypeBadge`, never imported anywhere
  — dead code.
- `telemetry-center.tsx` and `supervisor-brain.tsx` each independently redefine an identical
  `AXIS_PROPS` constant that belongs in `chart-card.tsx`.
- Loading/empty/error state handling is duplicated ad hoc per component (every data component
  hand-rolls its own three-state branch) rather than via a shared wrapper.
- `agent-inspector.tsx` and `agent-profile.tsx` both render a near-identical "metric tile" grid via
  separately defined `Metric`/`MiniStat` helpers — could share one primitive.

---

## Summary punch list (pre-1.1)

| Priority | Item | Service |
|---|---|---|
| High | Global exception-handling middleware (`KeyNotFoundException` → 404, else structured 500) | api |
| High | Handler/controller-level logging on error paths | api |
| High | React error boundary + Next.js `error.tsx` | frontend |
| High | Any test coverage at all | frontend |
| Medium | Surface TanStack Query read errors distinctly from empty states | frontend |
| Medium | Offload `PromptRegistry.render`'s blocking file read | ai-runtime |
| Medium | Move `handle_task_dispatched`'s payload parse inside the try/except boundary | ai-runtime |
| Medium | Extract the duplicated "load run by task node or 404" block | api |
| Low | Everything else above | all |

None of these are release blockers for a single-tenant, locally-demoed 1.0 — they're the first
hardening pass to schedule immediately after.
