# Changelog

All notable changes to this project are documented here. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.0.0] — Release 1.0

First public release. Everything below was built incrementally across five phases; this entry
consolidates them into one release rather than listing every intermediate commit.

### Added

**Backend (`api/`)**
- Clean Architecture solution: Domain, Application (CQRS via MediatR), Infrastructure (EF Core +
  Npgsql, Redis Streams), Api (controllers, SignalR hub, composition root).
- Workflow orchestration: workspaces, workflow runs, dynamic task DAGs (nodes + edges), a scheduler
  that dispatches ready nodes and checkpoints full DAG state after every pass.
- Agent registry with heartbeat-based availability tracking.
- Reasoning trace persistence (one row per pipeline stage, per task).
- Supervisor decision log (strategy selection, replan, retry, reassign, debate).
- Multi-layer memory (Working/Conversation/Project active; Workflow/LongTerm modeled).
- Versioned artifact storage with idempotency-key deduplication.
- Correlation IDs threaded through every event, trace, and decision.
- FluentValidation pipeline behavior + structured 400 responses.
- Execution snapshots (`Checkpoint`) — the foundation of Execution Playback.
- 12 controllers, 35 endpoints — see [docs/API.md](docs/API.md).

**AI Runtime (`ai-runtime/`)**
- Async agent framework (`AgentBase`) with 7 concrete specialist agents: Business Analyst, Project
  Manager, System Architect, Backend Engineer, Frontend Engineer, Code Reviewer, QA Engineer.
- 12-stage reasoning pipeline, uniformly applied to every agent invocation.
- Supervisor Brain: dynamic DAG expansion driven by task completion.
- Multi-model router (Anthropic/OpenAI/Gemini/Ollama) with a deterministic mock fallback — the
  entire system runs end-to-end with zero API keys configured.
- Sandboxed filesystem tool with defense-in-depth path-traversal/symlink-escape protection.
- File-based, versioned Prompt Registry.
- Structured failure classification (`StructuredFailure`) as a single, uniform error boundary.

**Frontend (`frontend/`) — Mission Control**
- Next.js 16 / React 19 dashboard, dark-first, communicating only with the .NET API and its
  SignalR hub (never the AI runtime directly).
- Dashboard, Workflow Runs, and a live Execution Graph (React Flow, custom layered DAG layout).
- Reasoning Inspector (click any node for its full 12-stage breakdown).
- Execution Playback — scrubs through real checkpoint history, not an animation.
- Agents registry + full agent profiles.
- Artifacts Explorer — Monaco code viewer, rendered Markdown, version history, Monaco diff view.
- Memory Inspector, Telemetry Center (11 charts), Supervisor Brain page, Prompt Registry.
- Command palette (Ctrl+K) searching agents/runs/artifacts/prompts, with a run-replay action.
- Export menu (execution summary, graph, artifacts, reasoning trace, telemetry — JSON/Markdown).
- Project Health — composite score computed from real execution data; categories with no real
  signal (Security, Maintainability) are reported as unmeasured, not guessed at.
- Portfolio Demo — one-click CTA that runs the real pipeline live, no configuration required.
- Mobile navigation, accessibility labels, responsive layout pass.

**Documentation & release engineering**
- Full architecture documentation set with 8 Mermaid diagrams (`docs/architecture/`).
- Complete API reference (`docs/API.md`).
- Code, Security, and Performance review reports (`docs/reviews/`).
- Deployment guide, development guide, roadmap, and FAQ.
- Demo package: scripts, slides, and a recruiter quickstart (`docs/demo/`).
- Open-source readiness: issue/PR templates, `CODE_OF_CONDUCT.md`, `SECURITY.md`,
  `CONTRIBUTING.md`, MIT `LICENSE`.

### Known limitations (see [docs/ROADMAP.md](docs/ROADMAP.md) for the full, honest list)

- No authentication or authorization anywhere in the system — intentional scope for this release,
  not an oversight; see [docs/reviews/SECURITY_REVIEW.md](docs/reviews/SECURITY_REVIEW.md).
- No global exception-handling middleware in the API (some error paths return a bare 500 instead
  of a proper 404).
- No frontend test suite yet.
- Vector Memory, Knowledge Graph, a Security Analyzer, and static-analysis-based Maintainability
  scoring are not implemented — the relevant UI surfaces say so explicitly rather than faking data.
