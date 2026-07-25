# Performance Review — Release 1.0

Scope: `api/` (Postgres/EF Core, Redis, SignalR), `ai-runtime/`, `frontend/` (data fetching,
rendering, DAG layout). Method: direct code inspection of query patterns, index coverage, and
rendering/polling strategy, cross-referenced against `PERFORMANCE_BASELINE.md`'s measured numbers
from the Phase 1.5 hardening pass.

**Overall assessment**: the platform performs well at the scale it's built and demoed at (single
operator, a handful of concurrent workflow runs, DAGs of a few dozen nodes). The findings below are
what starts to matter as any of those dimensions grow — none of them are visible today.

---

## 1. Database (PostgreSQL / EF Core)

### Real issue: `GetArtifactVersionsQuery` loads the entire `Artifacts` table
`Application/Artifacts/Queries/GetArtifactVersionsQuery.cs` walks a logical artifact's version
chain by doing `var all = await db.Artifacts.ToListAsync(cancellationToken);` — every row in the
table, every time a user opens the Versions or Diff tab on any artifact — and then walks the
`PreviousVersionId` chain in C# over the in-memory list. At demo scale (dozens of artifacts) this
is invisible. It will not scale: this is an O(total artifacts across every workspace and run) load
on every single click, not O(versions of this one artifact).

**Fix** (straightforward, since the index already exists — `ArtifactConfiguration` has
`HasIndex(x => x.PreviousVersionId)`): either (a) scope the initial query to
`WorkspaceId == <the artifact's workspace>` before loading, which bounds it to one workspace instead
of the whole table, or (b) replace the in-memory walk with a recursive CTE via
`FromSqlInterpolated`, which would resolve the whole chain in one indexed round-trip regardless of
table size. (a) is the lower-risk fix for 1.0; (b) is the correct long-term fix.

### Index coverage
11 of 14 EF Core entity configurations declare explicit indexes (`HasIndex`); the three without
(`ClarificationAnswerConfiguration`, `WorkflowDefinitionConfiguration`, `WorkspaceConfiguration`)
are all low-cardinality or rarely-filtered tables where a missing index isn't currently a hot-path
concern. `ArtifactConfiguration` in particular has a composite `(WorkspaceId, Name)` index plus a
`PreviousVersionId` index and a unique partial index on `(WorkflowRunId, IdempotencyKey)` — good
coverage for the query shapes that actually run today (everything except §above).

### Connection handling
`AddDbContext<ApplicationDbContext>` is registered without `AddDbContextPool` and without
`EnableRetryOnFailure`. Neither is wrong for current scale, but both are one-line changes worth
making before any multi-instance or higher-throughput deployment: `AddDbContextPool` reuses
`DbContext` instances instead of allocating one per request, and `EnableRetryOnFailure` absorbs
transient network blips against a managed Postgres instance instead of surfacing them as 500s.

### Other query patterns
Every other query handler reviewed (`GetWorkflowRunQuery`, `GetSupervisorSummaryQuery`,
`GetReasoningTelemetryQuery`, `GetMemoryOverviewQuery`) filters by an indexed column
(`WorkspaceId`/`WorkflowRunId`) before materializing, and uses `.Include()` rather than triggering
lazy-load N+1s. `GetReasoningTelemetryQuery`'s per-stage `GroupBy` aggregate runs as one SQL query
(EF Core translates `GroupBy`+aggregate to SQL correctly here — verified the LINQ shape doesn't fall
back to client evaluation), not N+1.

## 2. Redis

- Event bus (`RedisStreamsEventBus`) uses consumer groups (`XREADGROUP`) with per-message
  acknowledgment — the correct pattern for at-least-once delivery, and it fans the same stream out
  to three independent consumer groups (`orchestrator`, `signalr-relay`, `ai-runtime-agents`)
  without any group blocking another, so a slow SignalR relay can never back-pressure scheduling.
  This is a real strength, not just an implementation detail — it's the right design for this
  workload.
- No explicit `MAXLEN`/trimming policy was found on the stream — over a very long-running
  deployment with no restarts, the stream could grow unbounded in Redis memory. Not a concern for a
  locally-demoed 1.0 (Redis gets recreated with the container), but worth an explicit `MAXLEN ~`
  trim policy before any long-lived production deployment.

## 3. SignalR

`SignalRRelayHostedService` is efficient: it subscribes to its own consumer group, and on each
event does a direct `hub.Clients.Group(...).SendAsync(...)` with no database query in the hot path
— it relays the envelope fields it already has in memory. This scales with the number of *events*,
not the number of *connected clients per group* in any pathological way (SignalR's own group
fan-out handles that). No changes recommended at current scale.

One gap carried over from the Security Review: hub methods take an unvalidated `string`
`workflowRunId`, which is a correctness/security note, not a performance one — noted here only for
completeness.

## 4. Memory

- The AI runtime's `PromptRegistry` loads prompt files from disk per-invocation rather than caching
  them in memory after first read (see Code Review §2's blocking-I/O finding) — this is both a
  latency and a memory-churn concern under load, since every agent call re-reads and re-parses the
  same small set of prompt files. A simple in-memory cache keyed by file path (invalidated on
  registry reload, if that's ever needed) would remove both the blocking I/O and the repeated
  allocation.
- No unbounded in-memory collections were found growing across the process lifetime in either the
  API or the AI runtime — `RedisStreamsEventBus`'s in-flight task tracking and
  `AgentEventConsumer`'s equivalent are both correctly pruned via `add_done_callback`/task discard.

## 5. Workflow execution

- The Scheduler writes a full `Checkpoint` (complete node/edge snapshot, JSON-serialized) after
  **every** scheduling pass (`SchedulerService.BuildCheckpoint`). This is what makes Execution
  Playback possible, and it's cheap at current DAG sizes (a few dozen nodes serialize instantly),
  but it means checkpoint-table growth is proportional to *scheduling passes*, not *workflow runs* —
  a workflow with many small incremental dispatches will accumulate many checkpoint rows. Not a
  problem today; worth a retention/pruning policy (e.g. keep the last N checkpoints per run, or all
  of them but compress older ones) if very long-running or very frequently-rescheduled workflows
  become common.
- The reasoning pipeline's 12 stages run sequentially per agent invocation with no unnecessary
  cross-stage serialization observed; parallel task dispatch (e.g. Backend + Frontend together) is
  handled by the Supervisor's DAG structure, not blocked by the pipeline implementation.

## 6. Serialization

- Two distinct JSON conventions coexist by design (documented, not a bug): ASP.NET's MVC formatter
  emits camelCase for all HTTP API responses, while `Checkpoint.SnapshotJson` and the Redis
  `EventEnvelope` are serialized directly via `System.Text.Json.JsonSerializer.Serialize` and stay
  PascalCase. This is a real footgun for anyone extending the frontend (confirmed it caused one
  real bug earlier in this project's history, documented in the frontend's `types.ts` comments) —
  worth calling out in `API.md` explicitly so it isn't rediscovered the hard way again.
- No excessively large payloads were found — DTOs are field-selected (e.g. `ArtifactDto` doesn't
  round-trip the full EF entity), and list endpoints cap results with a `Limit` parameter
  (`GetWorkflowRunsQuery`, `GetArtifactsQuery`, `GetReasoningTelemetryQuery`'s `PointsLimit`, etc.).

## 7. Frontend rendering

- **DAG layout** (`lib/dag-layout.ts`) uses a longest-path column-relaxation algorithm bounded at
  `nodes.length` iterations over all edges — O(V·E) worst case. For the demo-scale DAGs this system
  produces today (single digits to low dozens of nodes), this runs in well under a millisecond. It
  has not been tested against DAGs of hundreds or thousands of nodes; at that scale the layout pass
  itself would still likely be fast enough (the constant factor is tiny), but **React Flow's
  unvirtualized rendering of every node as a real DOM element** would become the actual bottleneck
  first — React Flow does not virtualize off-screen nodes by default. This is the one true "large
  DAG scalability" concern: it's a rendering-library limitation, not this codebase's algorithm.
  Not worth solving pre-1.0 (no workflow in this system currently produces more than a few dozen
  nodes), but worth flagging in the Roadmap for anyone extending task granularity significantly.
- **Polling load**: four hooks poll on fixed intervals regardless of whether anything relevant is
  happening — `useAgents` every 10s, `useWorkflowRuns` every 5s, and
  `useMemoryOverview`/`useReasoningTelemetry`/`useSupervisorSummary` every 15s, all active for as
  long as their owning page is mounted. Reasonable for one operator's browser tab; with many
  concurrent dashboard viewers this becomes N× constant backend load independent of actual
  workflow activity, since SignalR already pushes live updates that trigger the same query
  invalidations. **Recommendation**: rely on the existing SignalR-driven `invalidateQueries` calls
  (`hooks/use-live-workflow.ts`) as the primary update mechanism and either drop or significantly
  lengthen these polling intervals — they're currently a belt-and-suspenders fallback that costs
  more than it should at multi-viewer scale.
- Monaco Editor and the diff viewer are lazy-loaded via `next/dynamic` with `ssr: false` — correct;
  they're only pulled into the bundle when an Artifacts Explorer tab that needs them is opened.
- No unnecessary full-list re-renders were found in the components reviewed; Zustand selectors are
  used consistently (`useXStore((s) => s.field)`) rather than subscribing to whole stores, which
  avoids the classic "any state change re-renders every consumer" trap.

## 8. Large-DAG scalability — summary

At today's scale (workflows producing single-digit-to-low-dozens of task nodes), no part of this
system is close to a performance ceiling — this was confirmed live: the full 7-node "Build a Task
Management SaaS" demo run completes in well under a minute end-to-end on the mock model provider,
including 12 reasoning stages per node, a full checkpoint per scheduling pass, and live SignalR
propagation to the dashboard. The two items that would need attention before this system was asked
to handle DAGs an order of magnitude larger are:

1. React Flow's lack of node virtualization (frontend rendering, §7).
2. `GetArtifactVersionsQuery`'s full-table load (database, §1) — this one *does* matter today, just
   not visibly, since it scales with total artifacts across all workspaces, not with DAG size.

---

## Summary

| Priority | Finding | Area |
|---|---|---|
| Medium | `GetArtifactVersionsQuery` loads the entire Artifacts table on every version/diff view | api / database |
| Medium | Frontend polling intervals are redundant with SignalR-driven invalidation at multi-viewer scale | frontend |
| Low | `PromptRegistry` re-reads prompt files from disk on every agent call, uncached | ai-runtime |
| Low | No `AddDbContextPool` / `EnableRetryOnFailure` on the EF Core registration | api |
| Low | No Redis stream trim/`MAXLEN` policy for very long-lived deployments | api |
| Low | Checkpoint table grows per scheduling pass with no retention policy | api |
| Low | React Flow has no node virtualization — a future large-DAG concern, not a current one | frontend |
| — (strength) | Redis Streams consumer-group fan-out cleanly decouples scheduling from SignalR relay | api |
| — (strength) | SignalR relay has no DB query in its hot path | api |
| — (strength) | DTOs are field-selected; list endpoints are all limit-capped | api |
