# Release Notes — v1.0.0

**AI Agents Team v1.0.0** — an autonomous AI software engineering company, with a live dashboard
that shows its work.

Submit a one-line goal — *"Build a Task Management SaaS"* — and watch a supervised fleet of seven
specialist agents turn it into requirements, architecture, backend and frontend code, a code
review, and a QA pass, coordinated through a dynamically generated task graph. Every reasoning
stage, supervisor decision, and produced artifact is observable live in **Mission Control**, the
system's own dashboard — and later, replayable exactly as it happened, from real checkpoint history.

This is the first tagged release. It closes out five build phases (architecture, working prototype,
production hardening, the Mission Control dashboard, and this release-engineering pass) into one
versioned, documented, demoable whole.

## Highlights

- **Zero-config demo**: `docker compose up -d && cd frontend && npm run dev`, then click **Run
  demo**. No API keys required — the Multi-Model Router runs the entire pipeline on a deterministic
  mock provider when none is configured, and every screenshot and recorded walkthrough in this
  repo was produced that way.
- **A dashboard that doesn't fake anything.** Every chart, score, and metric in Mission Control is
  computed from real execution data. Where there's no real signal — Security and Maintainability
  scores, Vector Memory, a Knowledge Graph — the UI says so explicitly instead of rendering an
  invented number.
- **Real Execution Playback**, not an animation: the scheduler checkpoints full DAG state after
  every scheduling pass, so scrubbing through a run's history shows what the graph actually looked
  like at each point in time.
- **A genuinely enforced architecture boundary**: the dashboard never talks to the Python AI runtime
  directly, and the AI runtime never touches the database directly — both checked in the included
  Code Review, not just documented.

## What's in this release

See [CHANGELOG.md](../CHANGELOG.md) for the full list. In short: the complete three-service
platform (`.NET` API, Python AI runtime, Next.js dashboard), 12 API controllers / 37 endpoints, 7
specialist agents, an 11-page Mission Control dashboard, and — new in this release specifically —
a full documentation set, a code/security/performance review, and open-source release scaffolding.

## Known limitations

Read before deploying anywhere beyond your own machine:

- **No authentication or authorization anywhere** — intentional scope for this release (a
  single-operator, local/demo deployment), not an oversight. See
  [docs/reviews/SECURITY_REVIEW.md](reviews/SECURITY_REVIEW.md) and
  [docs/DEPLOYMENT.md § Before deploying anywhere beyond localhost](DEPLOYMENT.md#before-deploying-anywhere-beyond-localhost).
- No frontend test suite yet.
- Some error paths in the API return a bare 500 instead of a proper 404 (global exception handling
  is on the immediate roadmap).

Full punch list: [docs/ROADMAP.md](ROADMAP.md).

## Upgrading

N/A — this is the first release.

## Thanks

Built end to end — architecture, backend, AI runtime, dashboard, and this release pass — as one
continuous project. See [docs/DEVELOPMENT.md](DEVELOPMENT.md) if you'd like to contribute to what's
next.

---

**Full documentation index**: [README](../readme.md) ·
[Architecture](architecture/OVERVIEW.md) · [API Reference](API.md) ·
[Deployment](DEPLOYMENT.md) · [Roadmap](ROADMAP.md) · [FAQ](FAQ.md)
