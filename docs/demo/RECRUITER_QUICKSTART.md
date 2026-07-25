# Recruiter Quick Start

**You have 2 minutes and no interest in running Docker locally.** Here's what to look at, in order:

1. **[Watch the recorded walkthrough](../video/mission-control-demo.webm)** (docs/video/) — a
   real, unscripted run of the actual application, not a mockup. ~2 minutes.
2. **[Screenshot tour](../../readme.md#mission-control-tour)** — every major page, in the README,
   no download required.
3. **[Architecture Overview](../architecture/OVERVIEW.md)** — one page, one diagram, explains the
   whole system in about 90 seconds of reading.

**If you do want to run it** (5 minutes, no API keys needed):

```bash
git clone https://github.com/Ali-Khamis45/Multi-Agent-orkflow-.git
cd Multi-Agent-orkflow- && docker compose up -d
cd frontend && npm install && npm run dev
```

Open `http://localhost:3000`, click **Run demo** on the dashboard. That's it — you're watching a
real multi-agent system plan, code-review, and QA a small SaaS product, live, coordinated by a
supervisor agent that builds its own execution plan as it goes.

## What this project demonstrates

- **Full-stack systems design**: three services (.NET, Python, Next.js) with a genuinely enforced
  architecture boundary between them — not just claimed in a README, actually checked (see the
  [Code Review](../reviews/CODE_REVIEW.md)).
- **Distributed systems fundamentals**: an event-bus-driven architecture (Redis Streams, consumer
  groups, at-least-once delivery handled idempotently), not a monolith with a job queue bolted on.
- **Real engineering discipline under a fast build**: this project shipped a working prototype,
  hardened it, built a full observability dashboard, and then produced an honest self-review
  finding its own bugs and gaps — including a subtle React/TypeScript trap
  ([Code Review §3](../reviews/CODE_REVIEW.md)) that silently broke a UI feature for an entire
  session before being caught and fixed.
- **Product thinking, not just implementation**: the Project Health page computes real scores from
  real data and explicitly refuses to fake the two categories it can't measure — that restraint is
  a deliberate choice, documented as one, not an accident.

## If you're evaluating for a specific role

- **Backend/.NET**: start with [docs/API.md](../API.md) and [Code Review §1](../reviews/CODE_REVIEW.md#1-api--aspnet-core).
- **Distributed systems / Python**: [docs/architecture/EVENT_BUS.md](../architecture/EVENT_BUS.md)
  and [Code Review §2](../reviews/CODE_REVIEW.md#2-ai-runtime--pythonfastapi).
- **Frontend/React**: [docs/architecture/OVERVIEW.md § Folder structure](../architecture/OVERVIEW.md#folder-structure)
  and [Code Review §3](../reviews/CODE_REVIEW.md#3-frontend--nextjs-mission-control).
- **Security**: [docs/reviews/SECURITY_REVIEW.md](../reviews/SECURITY_REVIEW.md) — written as a real
  finding-severity-recommendation document, not a checklist with everything marked green.
