# Contributing

Thanks for considering a contribution. This is a young, fast-moving project — the fastest way to
get context is [docs/architecture/OVERVIEW.md](docs/architecture/OVERVIEW.md) and
[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md), in that order.

## Before you start

- Check [docs/ROADMAP.md](docs/ROADMAP.md) — if what you want to build is already on it, open an
  issue first so effort doesn't collide.
- Check the [Code Review](docs/reviews/CODE_REVIEW.md) — some of what looks like a bug is a known,
  tracked gap; no need to re-report it, but a fix PR is very welcome.
- For anything nontrivial, open an issue describing the change before writing code. For small fixes
  (typos, an obvious bug with an obvious fix), a PR alone is fine.

## Development setup

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for running each service, and
[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for the full `docker compose` path.

## Conventions

Follow the patterns already in the codebase, not just "what compiles":

- **Backend**: CQRS via MediatR, one folder per feature under `Application/`, `Commands/`/`Queries/`
  inside. See any existing feature (e.g. `Application/Artifacts/`) as the template.
- **AI runtime**: a new agent is a declarative `AgentBase` subclass — don't duplicate pipeline/retry/
  telemetry logic that already lives on the base class.
- **Frontend**: data fetching only in `hooks/`, wrapping `lib/api-client.ts`; client UI state in
  Zustand `store/`; never call a backend host from anywhere but `lib/api-client.ts` or
  `lib/signalr.ts` — this boundary (dashboard never talks to the Python AI runtime directly) is
  load-bearing, not a style preference. See [docs/architecture/OVERVIEW.md](docs/architecture/OVERVIEW.md).

Full detail in [docs/DEVELOPMENT.md § Project conventions](docs/DEVELOPMENT.md#project-conventions).

## Before opening a PR

```bash
# Backend
cd api && dotnet build && dotnet test

# AI runtime
cd ai-runtime && pytest

# Frontend
cd frontend && npx tsc --noEmit && npm run lint
```

All three should be clean. If you're touching a UI page, actually run it (`npm run dev`) and look
at it — this project has no frontend test suite yet (see [Roadmap](docs/ROADMAP.md)), so a visual
check is the only verification that exists today.

## Commit style

Descriptive commit messages that explain *why*, not just *what* — see `git log` for the established
tone. Reference the relevant doc or review finding if your change addresses one.

## Pull requests

Use the PR template (`.github/PULL_REQUEST_TEMPLATE.md` — filled in automatically when you open a
PR). Keep PRs scoped to one concern; a PR that both fixes a bug and adds a feature is harder to
review and harder to revert if something's wrong.

## Reporting bugs / requesting features

Use the issue templates. Security issues are handled separately — see [SECURITY.md](SECURITY.md),
do not open a public issue for those.

## Code of Conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md). Participation implies agreement to it.
