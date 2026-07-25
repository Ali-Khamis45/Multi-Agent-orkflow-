# 5-Minute Walkthrough

A minute-by-minute guide for giving (or watching) a live walkthrough of Mission Control. Matches
the structure of [docs/video/mission-control-demo.webm](../video/mission-control-demo.webm).

## Setup (before you start the clock)

```bash
docker compose up -d
cd frontend && npm run dev
```

Open `http://localhost:3000`. Confirm the Dashboard loads with agent statuses showing `Available`.

## Minute 1 — Submit and watch it start

- Go to **Workflow Runs**. Type (or click the example chip) *"Build a Task Management SaaS"*, hit
  **Run**.
- You're redirected to the run's **Execution Graph** immediately — note it starts with exactly one
  node (`BusinessAnalysis`). Say: *"The graph isn't a template — the Supervisor builds it as work
  completes."*
- Within a few seconds (mock provider is fast), the graph grows: ProjectPlanning, then
  ArchitectureDesign, then **two nodes appear in the same column** — Backend and Frontend
  implementation, dispatched together because neither depends on the other.

## Minute 2 — Reasoning Inspector

- Click any completed node. The Inspector panel opens on the right: **12/12 reasoning stages**,
  each with duration, tokens, tool calls, memory reads. Say: *"Every agent invocation — regardless
  of which of the 7 agents — runs through the identical 12-stage pipeline. That uniformity is what
  makes this whole dashboard possible without agent-specific rendering code."*
- Point out the confidence percentage — trace it: *"That number is the ConfidenceEvaluation stage's
  actual output, not a display artifact."*

## Minute 3 — Playback and Supervisor

- Switch to the **Playback** tab. Hit play. Say: *"This isn't an animation between 'now' and 'the
  end' — the scheduler writes a full DAG snapshot after every scheduling pass. This is scrubbing
  through real history."*
- Switch to **Supervisor**. Show the decision log: *"Every DAG-expansion decision is recorded with
  a rationale and a confidence score, not just the outcome."*

## Minute 4 — The rest of Mission Control

- **Agents** page → click into a profile: live stats, model usage, cross-workflow reasoning
  timeline.
- **Artifacts Explorer**: open a Markdown artifact (rendered), then a code artifact (Monaco, syntax
  highlighted). Say: *"Every one of these is the agent's real output, not a placeholder — on the
  mock provider it's a deterministic stub, but the exact same rendering path handles a real LLM's
  output the moment an API key is set."*
- **Telemetry Center**: scroll through 2-3 charts — stage duration, confidence distribution.

## Minute 5 — Project Health and close

- **Project Health**: point at the two gray/unmeasured rings (Security, Maintainability). Say:
  *"These aren't given a fake score. There's no static-analysis subsystem built yet, so the page
  says so instead of guessing — same principle as the whole review process behind this release."*
- Close on the **Command Palette** (Ctrl+K): search across agents, runs, artifacts in one box.

## If something goes wrong mid-demo

- Graph stuck / nothing updating: check `docker compose ps` — most likely Redis or the API
  container isn't healthy. `docker compose logs api` for detail.
- Blank page / connection refused: confirm `npm run dev` is actually running and you're on
  `localhost:3000`, not a stale port from a previous session.
- Slow: the mock provider is fast; if it feels slow, check nothing else on the machine is
  contending for the Docker containers' CPU.
