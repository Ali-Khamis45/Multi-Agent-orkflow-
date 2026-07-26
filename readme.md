# AI Agents Team

**An AI Enterprise Platform with multiple Workspaces** — register and choose a Workspace, and a
supervised fleet of AI specialists runs your company for you. Today there are two: the original
**Software Company** (submit a one-line goal like *"Build a Task Management SaaS"* and watch it
become requirements, architecture, backend/frontend code, a code review, and a QA pass), and the
new **Founder Workspace** — a business operating system, not a dashboard. A guided onboarding
flow builds a persistent Company Profile once; from then on, 11 AI specialists (CEO, Business
Analyst, Market/Customer Research, Brand, Finance, Marketing, Operations, Sales, Growth, Legal)
already know the business on every request — no re-explaining, ever — and either run a full
venture-framing pass or, once the profile exists, answer a focused ask ("Create Instagram content",
"Should I increase prices?") with just the one relevant specialist. Every agent both reads from and
writes back to that Company Profile, and a real (never fabricated) Business Health score, gap-driven
recommendations, and a milestone timeline all derive from it. Both Workspaces run on the same shared
platform, coordinated through a dynamically generated DAG, with every decision, reasoning stage, and
artifact observable live in each Workspace's own dashboard.

<p align="center">
  <img src="docs/screenshots/03-execution-graph.png" alt="Mission Control — live execution graph" width="850">
</p>

<p align="center">
  <a href="docs/video/mission-control-demo.webm">▶ Watch the full walkthrough (docs/video/mission-control-demo.webm)</a>
</p>

---

## What this is

Three cooperating services:

- **`api/`** — ASP.NET Core 10 orchestration service (Clean Architecture,
  MediatR/CQRS, EF Core + PostgreSQL, SignalR). Owns all durable state
  (workflow runs, task graphs, artifacts, memory, telemetry) and is the
  **only** thing the frontend or the AI runtime is allowed to persist
  through.
- **`ai-runtime/`** — Python/FastAPI service that is the system's "brain":
  intent analysis, a 12-stage reasoning pipeline per agent invocation, the
  Supervisor Brain (which dynamically expands the task DAG as work
  completes), a multi-model router (falls back to a deterministic mock
  provider if no API key is configured — the whole system runs end-to-end
  with zero external dependencies), and seven specialist agents (Business
  Analyst, Project Manager, System Architect, Backend/Frontend Engineer,
  Code Reviewer, QA Engineer).
- **`frontend/`** — **Mission Control**, a Next.js 16 / React 19 dashboard.
  It talks *only* to the ASP.NET API and its SignalR hub — never to the
  Python runtime directly — and renders everything the platform does:
  the live execution graph, per-agent reasoning traces, supervisor
  decisions, artifacts, memory, telemetry, and more.

See [docs/architecture/OVERVIEW.md](docs/architecture/OVERVIEW.md) for the
full system design (with diagrams), [ARCHITECTURE.md](ARCHITECTURE.md) for
the original phased build order, [ARCHITECTURE_EXTENSION.md](ARCHITECTURE_EXTENSION.md)
for the 16-subsystem enterprise extension layer this was built additively
toward, [PHASE_1_5_HARDENING.md](PHASE_1_5_HARDENING.md) for the
production-hardening pass, and [PERFORMANCE_BASELINE.md](PERFORMANCE_BASELINE.md)
for measured performance numbers.

## Quick start

```bash
git clone https://github.com/Ali-Khamis45/Multi-Agent-orkflow-.git
cd Multi-Agent-orkflow-
docker compose up -d          # postgres, redis, api, ai-runtime
cd frontend
cp .env.example .env.local
npm install
npm run dev                   # http://localhost:3000
```

No API keys are required. `ai-runtime`'s Multi-Model Router runs entirely on
a deterministic mock provider unless `ANTHROPIC_API_KEY` / `OPENAI_API_KEY` /
`GEMINI_API_KEY` / `OLLAMA_HOST` is set (see `ai-runtime/.env.example`), so a
fresh clone can run a full pipeline immediately.

First visit redirects to `/register` — create an account and choose your
Workspace (Software Company or Founder Workspace). This choice is permanent:
it decides which dashboard, agents, and pipeline you get for that account.

**Fastest way to see it work:** open the dashboard and click **Run demo** on
the Portfolio Demo banner — it submits *"Build a Task Management SaaS"* to
the real intake pipeline and follows the run live. It completes in well
under a minute.

## Mission Control tour

| | |
|---|---|
| ![Dashboard](docs/screenshots/01-dashboard.png) **Dashboard** — live counts, agent fleet, recent activity, model usage. | ![Execution Graph](docs/screenshots/03-execution-graph.png) **Execution Graph** — the DAG, live via SignalR. Parallel branches (e.g. Backend + Frontend) render in the same column via a from-scratch layered layout. |
| ![Reasoning Inspector](docs/screenshots/04-execution-graph-inspector.png) **Reasoning Inspector** — click any node for its full 12-stage pipeline: tokens, tool calls, memory reads/writes, duration. | ![Execution Playback](docs/screenshots/05-playback.png) **Execution Playback** — scrub through a run's *real* checkpoint history (one snapshot per scheduling pass); the graph genuinely rebuilds itself at each historical state. |
| ![Agents](docs/screenshots/08-agents-grid.png) **Agents** — the registry, filterable by status/role/skill. | ![Agent Profile](docs/screenshots/09-agent-profile.png) **Agent Profile** — live stats, model usage, confidence trend, recent executions, cross-workflow reasoning timeline. |
| ![Artifacts Explorer](docs/screenshots/10-artifacts-explorer.png) **Artifacts Explorer** — GitHub-like browser: search, Monaco code viewer, rendered Markdown, version history, Monaco diff view. | ![Telemetry Center](docs/screenshots/12-telemetry-center.png) **Telemetry Center** — 11 charts: stage duration, agent duration, success/failure, confidence distribution, tool/memory usage, retries, token/model usage, workflow duration, DAG parallelism. |
| ![Supervisor Brain](docs/screenshots/13-supervisor-brain.png) **Supervisor Brain** — cross-run decision history, confidence evolution, decision-type breakdown, agent assignment load. | ![Project Health](docs/screenshots/15-project-health.png) **Project Health** — a composite engineering score computed from real execution data. Categories with no real signal (Security, Maintainability) are shown as unmeasured, never guessed at. |

Also included: a **Memory Inspector** (five-layer breakdown, relationships to
source artifacts, version/supersession history), a **Prompt Registry**
(every versioned prompt, its variables, and owning agent), a **Ctrl+K
command palette** that searches agents/runs/artifacts/prompts and can replay
a past run, and an **export menu** on every run (execution summary, graph,
artifacts, reasoning trace, and telemetry as JSON or Markdown).

## Tech stack

**Backend:** ASP.NET Core 10 · MediatR/CQRS · EF Core + Npgsql · FluentValidation · SignalR · Redis Streams (event bus)
**AI runtime:** Python 3 · FastAPI · async agent framework · multi-model router with mock fallback
**Frontend:** Next.js 16 (App Router) · React 19 · TypeScript · Tailwind CSS v4 · shadcn/ui (Base UI) · React Flow · Framer Motion · TanStack Query · Zustand · Monaco Editor · Recharts
**Infra:** PostgreSQL · Redis · Docker Compose

## Project structure

Full breakdown with rationale: [docs/architecture/OVERVIEW.md § Folder structure](docs/architecture/OVERVIEW.md#folder-structure).

```
api/            ASP.NET Core orchestration service (Clean Architecture)
ai-runtime/     Python FastAPI "brain" — agents, reasoning pipeline, Supervisor Brain
frontend/       Mission Control — Next.js dashboard
docs/           Everything below
```

## Documentation

| | |
|---|---|
| [Architecture Overview](docs/architecture/OVERVIEW.md) | System design, diagrams, folder structure |
| [Execution Flow](docs/architecture/EXECUTION_FLOW.md) · [Reasoning Engine](docs/architecture/REASONING_ENGINE.md) · [Supervisor Brain](docs/architecture/SUPERVISOR_BRAIN.md) | How a run actually executes |
| [Agent Lifecycle](docs/architecture/AGENT_LIFECYCLE.md) · [Memory](docs/architecture/MEMORY.md) · [Event Bus](docs/architecture/EVENT_BUS.md) · [Workflow Engine](docs/architecture/WORKFLOW_ENGINE.md) | Core subsystems |
| [API Reference](docs/API.md) | Every endpoint, DTO, error shape |
| [Deployment](docs/DEPLOYMENT.md) · [Development Guide](docs/DEVELOPMENT.md) | Running this for real / hacking on it |
| [Code Review](docs/reviews/CODE_REVIEW.md) · [Security Review](docs/reviews/SECURITY_REVIEW.md) · [Performance Review](docs/reviews/PERFORMANCE_REVIEW.md) | An honest, file:line-cited Release 1.0 audit |
| [Roadmap](docs/ROADMAP.md) · [FAQ](docs/FAQ.md) | What's next, common questions |
| [Demo package](docs/demo/) | Scripts, slides, and a recruiter quickstart |
| [Release Notes v1.0.0](docs/RELEASE_NOTES_v1.0.0.md) · [Changelog](CHANGELOG.md) | What shipped |
| [Retrospective](docs/RETROSPECTIVE.md) | What went well, key decisions, bugs found, lessons learned |
| [Contributing](CONTRIBUTING.md) · [Security Policy](SECURITY.md) · [Code of Conduct](CODE_OF_CONDUCT.md) | Project governance |

## License

[MIT](LICENSE)
