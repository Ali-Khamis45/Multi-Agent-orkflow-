# Phase 1.5 — Core Platform Hardening

This document records what changed between Milestone 2 (the working
end-to-end prototype) and this hardening pass: a production-quality
execution platform, verified against real infrastructure (Postgres, Redis,
and — for the .NET side — Testcontainers) rather than mocks. Every item
below maps to one of the 12 numbered requirements for this milestone.

No architectural component from `ARCHITECTURE.md` / `ARCHITECTURE_EXTENSION.md`
was redesigned or replaced — this pass extends the existing DAG Scheduler,
Event Bus, Reasoning Engine, and Agent Registry with the columns, tables,
and cross-cutting concerns listed here.

---

## 1. Observability — unified execution telemetry

`ReasoningTrace` (already the per-stage-per-task record from Milestone 2) is
now the single unified telemetry row Phase 1.5 asked for. Every stage of
every agent invocation persists:

`TaskNodeId, WorkflowRunId, CorrelationId, Agent, Stage, StartedAt,
DurationMs, Tokens, Confidence, ModelUsed, RetryCount, MemoryReads,
MemoryWrites, ToolCalls, CostEstimate, ErrorMessage`

Shaped deliberately to map onto OpenTelemetry span concepts
(`WorkflowRunId`/`CorrelationId` ~ trace id, `TaskNodeId`+`Stage` ~ span id,
`StartedAt`/`DurationMs` ~ span timing, everything else ~ span attributes) —
a future OTel exporter can translate this table into spans without a schema
change. Every stage also emits a structured JSON log line with the same
field set (`app/logging_config.py`), so today's grep-the-logs workflow and
tomorrow's OTel collector read the same information.

**Verified**: a single agent invocation now produces exactly 12
`ReasoningTrace` rows (previously 11 — `PublishResult` wasn't traced), with
real per-stage `ToolCalls`/`MemoryReads`/`MemoryWrites`/`ModelUsed`/`Tokens`,
not placeholders.

## 2. Correlation IDs

`CorrelationId` is minted once, by the Supervisor, at the true start of an
execution (`SupervisorAgent.kickoff`), and threads through every record that
execution produces:

- `WorkflowRun.CorrelationId` — the root.
- `TaskNode.CorrelationId` — inherited automatically from the parent
  `WorkflowRun` at creation (no extra parameter needed from callers).
- `Artifact`, `ReasoningTrace`, `SupervisorDecision`, `ExecutionFailure`,
  `Checkpoint` — derived **server-side** from the `WorkflowRun`/`TaskNode`
  they belong to wherever that relationship already exists, rather than
  trusted from the caller (fewer places for the AI Runtime to get it wrong).
- `IntentSession`, `MemoryItem` — accepted explicitly, since they may exist
  before or independent of a specific task's context.
- `EventEnvelope.CorrelationId` — every Redis Streams event carries it, so a
  single execution is traceable across ASP.NET, Redis, the Python Runtime,
  and SignalR (the four systems named in this requirement); the field is
  already there for "future workers" to pick up unchanged.

A complementary **per-HTTP-request** correlation ID (`CorrelationIdMiddleware`,
`X-Correlation-Id` header, echoed on the response, pushed into the logging
scope) covers requests with no workflow context at all — e.g. a bare
registry call.

**Verified**: `GET /api/workflows/runs/{id}` now returns a `correlationId`
field; a full pipeline run showed the same `CorrelationId` on the
`WorkflowRun` and on every `Checkpoint` row it produced.

## 3. Idempotency

- **`CompleteTaskCommand`/`FailTaskCommand`**: now guard on `TaskNode.Status`
  — only a node still `Dispatched`/`Running` can be completed or failed. A
  redelivered event (Redis Streams is at-least-once) for an already-resolved
  node is a no-op, not a double-decrement of the agent's in-flight count or
  a double-retry.
- **`AddTaskNodeCommand`**: checks for an existing node with the same `Name`
  within the run before creating one; a unique `(WorkflowRunId, Name)` index
  is the database-level backstop against a race between two concurrent
  calls.
- **`AddTaskDependencyCommand`**: checks for an existing identical edge
  before creating one.
- **`CreateArtifactCommand`**: accepts an optional `IdempotencyKey`
  (unique per `(WorkflowRunId, IdempotencyKey)`); a retried
  `produce_artifact` call — the Python agents now always pass
  `{task_node_id}:{artifact_name}` — resolves to the same artifact instead
  of spawning a spurious extra version.
- **Retry determinism**: `FailTaskCommand` now retries only when the
  Structured Failure's `Retryable` flag says so (see §4) — a permission
  error fails the run immediately regardless of remaining attempts, while a
  transient one still gets the configured number of attempts.

**Verified**: a live test published the same idempotency-keyed artifact
creation twice and got the same `ArtifactId` back both times; a
non-retryable structured failure went straight to `Failed` with
`AttemptCount = 1` even though `MaxTaskAttempts = 2`.

## 4. Structured error model

A new `ExecutionFailure` table (`TaskNodeId, WorkflowRunId, CorrelationId,
Agent, Category, Severity, Recoverable, Retryable, Message, Stack,
SuggestedAction`) replaces "a free-text exception string in `ReasonJson`."

The AI Runtime's `app/reasoning/failures.py` classifies every exception the
Reasoning Engine catches — `ToolError` → `Validation`/not retryable; HTTP
403/401 → `Permission`/not retryable; HTTP 429/5xx → `Provider`/retryable;
HTTP 4xx → `Validation`/not retryable; connection/timeout errors →
`Transient`/`Timeout`, retryable; anything else → `Unknown`, retryable by
default — and publishes that shape as the `TaskFailed` event's `ReasonJson`.
`FailTaskCommandHandler` parses it into the durable table, falling back to
`Unknown`/retryable for any legacy or malformed payload rather than
throwing.

**Bug found and fixed during this milestone**: the Python side originally
serialized `suggested_action` (its dataclass field name, snake_case); the
.NET parser expected `suggestedAction` (camelCase). Every structured failure
silently lost its `SuggestedAction`. Fixed in `StructuredFailure.
to_reason_json_dict()`; covered by `tests/test_failures.py::
test_wire_shape_uses_camel_case_suggested_action`.

## 5. Execution snapshots

`Checkpoint` (defined but unused in Milestone 2) is now written by
`SchedulerService` after **every** scheduling pass — labeled
`"scheduling-pass"`, or `"workflow-completed"` on the pass that finishes the
run — capturing the full `WorkflowRun` + every `TaskNode`'s id/name/status/
agent/confidence/risk/attempts, plus every edge, as JSON.

This is deliberately built on the table `ARCHITECTURE.md §9.2` already
defined — Resume/Replay/Fork/Debugging (the full §9.2 semantics) can be
implemented later purely by reading this table, no storage redesign.

**Verified**: a 7-node pipeline run produced 8 `scheduling-pass` checkpoints
plus 1 `workflow-completed` checkpoint, all sharing the run's
`CorrelationId`.

## 6. Agent metrics

`GetAgentMetricsQuery` (`GET /api/observability/agents/metrics`) aggregates
directly over `TaskNode` (grouped by `AssignedAgentName`) and
`ReasoningTrace` (grouped by `Agent`) — no separate metrics-tracking system;
the metrics are a read model over data the pipeline already produces, per
this requirement's explicit framing ("these metrics should already exist
before the dashboard"):

`TotalTasks, CompletedTasks, FailedTasks, SuccessRate, FailureRate,
AvgAttemptCount (retry rate proxy), AvgConfidence, AvgStageDurationMs,
ToolCallCount, MemoryReadCount, MemoryWriteCount, ModelUsage (by model)`

`GetAgentConfidenceTrendQuery` (`GET /api/observability/agents/{name}/
confidence-trend`) returns raw `(timestamp, confidence)` points, since a
single average can't show drift over time.

**Verified**: after one pipeline run, `business-analyst`'s metrics correctly
showed `toolCallCount: 2`, `memoryReadCount: 2`, `memoryWriteCount: 1`,
`modelUsage: {"mock-deterministic": 2}`.

## 7. Tool sandbox

`app/tools/sandbox.py` is now the single shared implementation every
filesystem-touching tool uses (today: `FilesystemTool`; any future tool
inherits the same model rather than re-implementing containment logic):

- Rejects absolute input paths outright.
- Rejects any `..` path segment before resolution (defense in depth).
- Resolves symlinks and re-checks containment against the *resolved* root,
  so a symlink planted inside the sandbox can't point outside it.
- Enforces a configurable maximum write size (`FILESYSTEM_MAX_BYTES`,
  default 1MB) to prevent disk-exhaustion abuse.

**Verified** (`tests/test_sandbox.py`): traversal, absolute-path escape, and
oversized-write are all rejected with a clear `ToolError`; symlink escape is
covered too (skipped automatically on platforms without symlink privileges,
e.g. Windows without Developer Mode — runs for real in the Linux container).

## 8. Prompt registry

Prompts moved from "flat `.txt` files read by name" to a centralized,
versioned registry (`app/prompts/registry.json` + the template files
alongside it, loaded by `app/tools/prompt_registry.py`). Every prompt now
exposes `version, description, variables, owner, compatible_agent`, and a
`versions` history list — adding a new version or rolling back to an old one
is a JSON + file change, never a code change. `PromptLoaderTool` accepts an
optional `version` parameter to render an older version explicitly.

A DB-backed `prompt_templates` table with full A/B experiment tracking
(`ARCHITECTURE.md §14.2`) remains a Phase 2+ upgrade — this file-based
registry implements the same contract now, so `PromptLoaderTool`'s callers
don't change when that upgrade lands.

## 9. Configuration layer

Audited both runtimes for hardcoded operational values:

- **Python**: `settings.self_base_url` (was `"http://ai-runtime:8000"`
  hardcoded in `main.py`), `settings.workspace_files_root` (was
  `"/data/workspace-files"`), `settings.filesystem_max_bytes`,
  `settings.environment` (`Local | Development | Testing | Docker |
  Production | Cloud`). `.env.example` documents every variable.
- **.NET**: `SchedulerOptions.MaxTaskAttempts` (was `const int MaxAttempts
  = 2` in `FailTaskCommandHandler`) is now bound from the `"Scheduler"`
  configuration section, settable per environment via `appsettings.json` or
  an environment variable.
- `docker-compose.yml` sets the Docker-specific values explicitly per
  service; nothing is baked into the images themselves.

## 10. API contract validation

A `ValidationBehavior<TRequest,TResponse>` MediatR pipeline behavior now
runs every command through its registered FluentValidation validators
before the handler executes — invalid payloads are rejected uniformly, with
a `ValidationExceptionMiddleware` translating the resulting
`ValidationException` into a structured `400 Bad Request` instead of a raw
`500`. Validators added for the highest-traffic commands: agent
registration, workflow-run creation, DAG node creation, artifact creation,
memory writes, and reasoning-trace recording.

(This finally puts the `FluentValidation.DependencyInjectionExtensions`
package — installed in Milestone 1, never wired up — to use.)

On the Python side, FastAPI's Pydantic models (`IntakeRequest`) already
validate the one external-facing boundary; internal AI-Runtime-to-.NET
calls rely on the .NET side's validation as authoritative, since it's the
system of record.

## 11. Integration tests

**`.NET`** (`api/Tests/AiAgentsTeam.IntegrationTests`): xUnit + Testcontainers
spins up real, ephemeral Postgres and Redis containers per test run — no
mocked persistence, no mocked Event Bus. 15 tests, all passing, covering
every scenario this requirement named: Agent Registration (register,
re-register/update, heartbeat), Workflow Creation, Intent Intake, Dynamic
DAG (idempotent node creation), Parallel Execution (two independent
branches dispatched together after a shared predecessor completes),
Supervisor Decisions (correlation derived from the run), Artifacts
(versioning, by-name resolution, idempotency), Memory (write/recall
round-trip), Reasoning Traces (full telemetry fields persisted and
retrievable), Retries (retryable failures retry up to the configured limit
then fail the run; non-retryable failures skip straight to terminal
failure), and Execution Snapshots (checkpoints written on every scheduling
pass, including the terminal one).

**Python** (`ai-runtime/tests/`): 40 passed, 1 skipped (platform-limited).
Pure-logic coverage with lightweight fakes standing in for the API
client/memory/event bus/model router (no network needed) — the Reasoning
Pipeline's 12-stage sequencing and telemetry counters, the Tool Registry's
permission enforcement, the Multi-Model Router's fallback/preference-order
behavior, the Prompt Registry's versioning, the Structured Failure
classifier, and the Filesystem sandbox's traversal/size defenses.

Run with:
```bash
# .NET
dotnet test api/Tests/AiAgentsTeam.IntegrationTests

# Python
cd ai-runtime && pip install -e ".[test]" && pytest tests/ -v
```

**Bugs these tests would have caught immediately** (both were actually
found via live manual testing first, then covered by a regression test):
the broken LINQ `ValueTuple` projection in `RecordReasoningTraceCommandHandler`
(§ below), and the `suggestedAction` casing mismatch (§4).

## 12. Performance baseline

See `PERFORMANCE_BASELINE.md` for the full numbers. Headline: a complete
7-node pipeline run (Intent → BA → Supervisor-built DAG through PM →
Architect → parallel Backend/Frontend → Review → QA), mock model provider,
completes in **1.54 seconds** end-to-end; `POST /intake` itself returns in
**77ms**; the two parallel nodes dispatch within **2.1ms** of each other,
concretely demonstrating §5.2 step 3's "independent branches land in the
same ready set."

---

## Bugs found and fixed during this hardening pass

Both were caught by dogfooding the new telemetry/failure-model live against
the running stack, before the automated tests existed — the tests above now
guard against regressions of both:

1. **Every enum crossed the wire as an integer, silently.** (Actually found
   at the very start of this pass, not specific to Phase 1.5 additions, but
   worth restating: `JsonStringEnumConverter` was already fixed in
   Milestone 2 — mentioned here only because it's the same *class* of bug
   as #2 below: a serialization-contract mismatch between the two runtimes
   that only surfaces at runtime, never at compile time.)
2. **Broken LINQ projection**: `RecordReasoningTraceCommandHandler` chained
   `.Select(n => new {...}).Select(n => ValueTuple.Create(...))` — EF Core's
   SQL translation of the double projection didn't line up with the
   materialization, throwing `GetFieldValue` errors on every single call.
   Every reasoning trace write failed and was silently swallowed by the
   Python pipeline's fire-and-forget error handling — telemetry looked
   "empty" rather than "broken." Fixed by projecting directly to an
   anonymous type and reading its properties, no `ValueTuple` involved.
3. **`suggestedAction` casing mismatch** (§4 above).

Neither broke the *primary* pipeline (workflows still completed
successfully) — both were observability/error-detail regressions, which is
exactly the category of bug a hardening pass exists to catch before it
reaches a dashboard consumer.

## What's still deferred (unchanged from Phase 1 build order, §24)

DevOps/Documentation/Security/Performance/UX-review agents, Agent
Marketplace, Distributed Execution, Learning Engine, Debate Mode, and full
Checkpoint Replay/Rollback/Fork execution remain out of scope — this
milestone hardened the *existing* Phase 1 surface, it didn't expand it.
