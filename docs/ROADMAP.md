# Roadmap

This is deliberately specific rather than aspirational — every item below is either a concrete gap
found during the Release 1.0 review pass, or a subsystem named in
[ARCHITECTURE_EXTENSION.md](../ARCHITECTURE_EXTENSION.md) that genuinely has zero implementation
yet. Nothing here is claimed as "in progress" unless it is.

## Immediate (first post-1.0 hardening pass)

From the [Code Review](reviews/CODE_REVIEW.md), [Security Review](reviews/SECURITY_REVIEW.md), and
[Performance Review](reviews/PERFORMANCE_REVIEW.md):

- [ ] Global exception-handling middleware in the API (`KeyNotFoundException` → 404, else
      structured 500) — currently 11 call sites can surface a bare 500.
- [ ] Handler/controller-level logging on error paths — `ILogger` is used in only 4 files today.
- [ ] Authentication + authorization — JWT bearer on the API, `[Authorize]` on the SignalR hub,
      workspace-ownership checks in handlers. Currently there is none, anywhere, by design for this
      release's local/demo scope.
- [ ] Input validation on `POST /api/intake` (no length cap today) and the other 22 of 30
      commands/queries with no FluentValidation validator.
- [ ] Fix `GetArtifactVersionsQuery`'s full-table load — scope it by workspace before the in-memory
      walk, or replace with a recursive CTE.
- [ ] React error boundary + Next.js `error.tsx` — a render exception currently crashes to the
      framework's default overlay.
- [ ] Any frontend test coverage at all — there is currently zero.
- [ ] Surface TanStack Query read errors distinctly from empty states (currently indistinguishable).
- [ ] Non-root `USER` in both Dockerfiles.

## Near-term

- **Vector Memory** — the `MemoryItem` schema already carries a `Score` field specifically so
  embedding-similarity ranking can be added as a new retrieval strategy over the existing table,
  not a redesign. Retrieval today is recency-ordered only. See
  [Memory](architecture/MEMORY.md#retrieval-today-vs-planned).
- **Workflow + LongTerm memory layers** — modeled in the schema, no current writer.
- **Retry/Replan wiring completeness** — `WorkflowRun.HasUnrecoverableFailure()` and several
  `TaskNode` state-transition methods (`Pause`, `WaitForApproval`, `Block`) exist but aren't yet
  called from any failure path; the demo dataset has never hit a scenario that needed them.
- **Frontend Dockerfile** — the frontend isn't containerized yet; `docker-compose.yml` intentionally
  omits it so the compose stack always builds cleanly at every commit rather than blocking on
  frontend build stability.
- **Contract/HTTP-layer tests for the API** — the existing integration tests call handlers directly
  via `ISender`; there are no `WebApplicationFactory` tests exercising controllers, middleware, CORS,
  or the SignalR hub.

## Longer-term — [ARCHITECTURE_EXTENSION.md](../ARCHITECTURE_EXTENSION.md) subsystems

Sixteen subsystems (E0–E16) were designed as an additive extension layer on top of the core
architecture. Status of each, honestly:

| # | Subsystem | Status |
|---|---|---|
| E1 | Supervisor Brain | **Built** — dynamic DAG expansion, decision types, decision history. |
| E2 | Intent Engine | **Built** — intent sessions, clarification, structuring. |
| E3 | Hierarchical Task Planner | **Partial** — the Supervisor's DAG expansion covers flat sequencing + parallel branches; no distinct sub-plan hierarchy exists yet. |
| E4 | Execution Graph | **Built** — dynamic DAG, live via SignalR, checkpointed for playback. |
| E5 | Multi-Layer Memory | **Partial** — 3 of 5 layers active; see Near-term above. |
| E6 | Reasoning Engine | **Built** — the 12-stage pipeline, uniformly applied. |
| E7 | Multi-Model Router | **Built** — 4 providers + deterministic mock fallback. |
| E8 | Agent Collaboration Protocol | **Not built** — the `Debate` supervisor-decision type is modeled and reserved but never triggered. |
| E9 | Workflow Template Library | **Not built** — every run currently originates from a fresh intent analysis, not a reusable template. |
| E10 | Learning Engine | **Not built.** |
| E11 | Observability 2.0 | **Mostly built** — Telemetry Center, per-stage reasoning traces, agent metrics. Not yet built: distributed tracing export (OpenTelemetry), cost-tracking beyond a per-trace `CostEstimate` placeholder. |
| E12 | AI Governance | **Not built** — no approval-gate enforcement or policy engine; the domain has scaffolding (`TaskNode.MarkWaitingApproval`) but no handler calls it. |
| E13 | Agent Marketplace | **Not built.** |
| E14 | Autonomous Improvement Loop | **Not built.** |
| E15 | Project Health Intelligence | **Partial** — Mission Control's Project Health page computes Reliability, AI Confidence, Testing, Architecture, Documentation, and Performance from real execution data; Security and Maintainability are explicitly reported as unmeasured (no Security Analyzer or static-analysis integration exists) rather than estimated. |
| E16 | Future Distributed Execution | **Not built** — agents run in-process within the single `ai-runtime` service; the agent registry's `endpoint` field is wired for this but not load-bearing yet. |

## Explicitly out of scope for 1.0

- Multi-tenancy / per-user auth (see Immediate, above — auth itself is planned; multi-tenant data
  isolation is a larger follow-on).
- A published, versioned public API (the current contract is intentionally coupled to this one
  frontend, not yet designed for third-party consumers — see [API Reference](API.md)).
- Any of the E8–E14/E16 subsystems above.
