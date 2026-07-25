# Development Guide

## Prerequisites

- .NET SDK 10
- Python 3.12+
- Node.js 20+
- Docker (for Postgres/Redis, or the full stack)

## Running each service independently

### `api/`

```bash
cd api
dotnet restore
dotnet build                                    # AiAgentsTeam.slnx
dotnet run --project Api                        # http://localhost:5080 (Development env)
```

Requires Postgres + Redis reachable at the ports in `Api/appsettings.Development.json`
(`localhost:5434` / `localhost:6380` by default — start just those two with
`docker compose up -d postgres redis`).

### `ai-runtime/`

```bash
cd ai-runtime
python -m venv .venv && .venv/Scripts/activate   # or source .venv/bin/activate on macOS/Linux
pip install -e ".[test]"
cp .env.example .env                             # edit if not using docker-compose defaults
uvicorn app.main:app --reload --port 8000
```

### `frontend/`

```bash
cd frontend
npm install
cp .env.example .env.local
npm run dev                                      # http://localhost:3000 (Turbopack)
```

> **Read `frontend/AGENTS.md` before writing App Router code.** This project pins Next.js 16 /
> React 19, both recent enough that framework conventions may differ from older training data —
> the bundled docs at `frontend/node_modules/next/dist/docs/` are the source of truth for anything
> App-Router-related that looks surprising.

## Running tests

```bash
# .NET — integration tests via Testcontainers (needs Docker running)
cd api && dotnet test

# Python — fake-based unit tests, no live network/DB needed
cd ai-runtime && pytest

# Frontend — no test suite exists yet (see Code Review); `tsc` and `eslint` are the current checks
cd frontend && npx tsc --noEmit && npm run lint
```

## Project conventions

- **CQRS everywhere in `Application/`**: one folder per feature, `Commands/` and `Queries/` inside,
  each file self-contained (record + handler together, not split across files). Follow this
  structure for any new backend feature — it's consistent across all 10 existing features.
- **DTOs are hand-written records**, not auto-mapped — see any `Queries/Get*Query.cs` for the
  pattern of a shared `ToDto` static method reused across a feature's handlers.
- **Validators are opt-in via FluentValidation**, auto-discovered by
  `AddValidatorsFromAssembly` and run by the `ValidationBehavior` pipeline — add an
  `AbstractValidator<TCommand>` next to any command that needs one; no wiring required beyond that.
- **Python agents are declarative**: a new agent is a subclass of `AgentBase` with class attributes
  for name/skills/tasks/priority and one `execute_domain_logic` method — do not duplicate
  pipeline/retry/telemetry logic from `AgentBase`.
- **Frontend data fetching only happens in `hooks/`**, never inline in a component — every hook
  wraps `lib/api-client.ts`, never calls `fetch` directly. `lib/api-client.ts` is the *only* file
  allowed to reference a backend host.
- **Client-only UI state lives in `store/` (Zustand)**, read via `useXStore((s) => s.field)`
  selectors, never a whole-store subscription.
- **Never let the frontend or `ai-runtime` touch a database directly.** The frontend talks only to
  the .NET API and its SignalR hub; the AI runtime persists only through the API's HTTP endpoints.
  Both boundaries are checked in the [Code Review](reviews/CODE_REVIEW.md) — keep them true.

## Commit & branch conventions

This repository uses conventional, descriptive commit messages explaining *why* a change was made,
not just what changed — see the git log for the established style. Release work happens on a
`release/x.y.z` branch, merged to `main` once validated (see
[Release Checklist](RELEASE_CHECKLIST.md)).

## Where to look first

New to this codebase? Read in this order: [Architecture Overview](architecture/OVERVIEW.md) →
[Execution Flow](architecture/EXECUTION_FLOW.md) → whichever subsystem doc matches what you're
touching → [API Reference](API.md) if you're changing an endpoint contract →
[Code Review](reviews/CODE_REVIEW.md) for known rough edges before you go looking for them yourself.
