# Presentation Script

A longer, talk-style narrative (~10-12 minutes spoken) for presenting this project to a technical
audience — pairs with [ARCHITECTURE_SLIDES.md](ARCHITECTURE_SLIDES.md). For a shorter hands-on-
keyboard demo, use [DEMO_SCRIPT.md](DEMO_SCRIPT.md) instead.

---

### 1. The problem (30s)

"Multi-agent AI systems are everywhere right now, but most demos show you a chat transcript. You
don't see *why* the system made a decision, you don't see what it tried and discarded, and you
definitely can't go back and inspect what happened three steps ago. I wanted to build a system
where none of that is hidden — where the orchestration itself is a first-class, observable thing,
not a side effect."

*[Slide: title]*

### 2. What this is (60s)

"AI Agents Team is an autonomous software engineering company. You give it a one-line goal — 'Build
a Task Management SaaS' — and a supervisor agent coordinates seven specialist agents through a task
graph it builds dynamically: requirements, architecture, backend, frontend, review, QA. Mission
Control, the dashboard, shows every part of that live."

*[Slide: what it does — screenshot of the Execution Graph]*

### 3. Why three services (90s)

"Three cooperating services, each doing one job. A .NET API that owns all durable state — it's the
only thing anything else is allowed to persist through. A Python runtime that's the system's
brain — intent analysis, the reasoning pipeline, the supervisor, the model router — and critically,
it never touches the database directly, only the API's HTTP endpoints. And a Next.js dashboard that
never talks to Python directly either — everything routes through the API, including two endpoints
that exist purely to proxy what the dashboard needs from the AI runtime, server-to-server.

That boundary matters because it means there's exactly one source of truth for what happened, which
is what makes the next part possible."

*[Slide: system diagram from docs/architecture/OVERVIEW.md]*

### 4. The reasoning pipeline (90s)

"Every agent, every task, runs through the same twelve-stage pipeline — observe, understand, think,
plan, retrieve context, retrieve memory, select tools, execute, reflect, self-critique, evaluate
confidence, publish. Every stage is individually timed and persisted. That uniformity is what lets
the dashboard render *any* agent's work with the same inspector, without agent-specific code — and
it's what makes the telemetry dashboard's per-stage charts possible, because every data point is
apples-to-apples across all seven agents."

*[Slide: 12-stage diagram from docs/architecture/REASONING_ENGINE.md]*

### 5. Execution Playback — the part I'm proudest of (90s)

"The scheduler snapshots the entire DAG — every node's status, every edge — after every single
scheduling pass. That was originally built for a future resume/replay feature. But it turned out to
be exactly what a playback UI needs: when you scrub through a run's history in Mission Control,
you're not watching an animation interpolate between the start state and the end state. You're
watching nine real historical snapshots, in order. The graph at step one genuinely only has one node
in it, because that's what it looked like."

*[Slide: Playback screenshot / gif]*

### 6. The honesty principle (60s)

"One decision I want to call out specifically: the Project Health page computes real composite
scores — reliability, confidence, testing, architecture, documentation, performance — from actual
execution data. But it has two categories, Security and Maintainability, where I don't have a real
signal, because there's no static-analysis or vulnerability-scanning subsystem built yet. Those
show as explicitly unmeasured, not a fabricated number. I applied that same principle to this whole
release: the code, security, and performance reviews in the repo aren't marketing copy — they're
real findings with file and line citations, including bugs I found in my own code."

*[Slide: Project Health screenshot, the two gray rings]*

### 7. What I'd build next (45s)

"The roadmap is specific, not aspirational — sixteen extension subsystems were designed up front,
and the docs say plainly which are built, which are partial, and which don't exist yet. Top of the
list for right now: authentication, since there's currently none anywhere in the system — that's a
deliberate scope boundary for this release, not something I missed. After that, a global exception
handler in the API, and frontend test coverage."

*[Slide: roadmap table from docs/ROADMAP.md]*

### 8. Close (30s)

"Everything I've shown you is running on a deterministic mock model provider — zero API keys, zero
external dependencies, `docker compose up` and it works. That was deliberate: I wanted anyone
evaluating this to be able to see the real system in under five minutes, not a slide deck describing
what it would do if they set it up. Questions?"

*[Slide: quickstart command + repo link]*
