# Demo Script (live, interactive)

For driving a live, hands-on-keyboard demo — an interview, a stakeholder walkthrough, a
conference booth. Assumes the stack is already running (`docker compose up -d` +
`npm run dev` from `frontend/`). For a fixed-length narrated version, see
[WALKTHROUGH_5MIN.md](WALKTHROUGH_5MIN.md); for slide talking points, see
[PRESENTATION_SCRIPT.md](PRESENTATION_SCRIPT.md).

## Opening line

> "This is an autonomous AI software engineering company. I'm going to give it one sentence, and
> you're going to watch seven specialist agents plan, build, review, and QA a small product —
> live, with a supervisor coordinating them, and nothing pre-recorded."

## Step 1 — The ask

Open the **Dashboard**. Point at the **Portfolio Demo** banner (or the Workflows page's example
chips). Click **Run demo** / submit *"Build a Task Management SaaS"*.

*Talking point*: "No API key is configured right now — this runs entirely on a deterministic mock
model provider. The exact same code path handles a real Claude/GPT/Gemini call the moment a key is
set; I'm not hiding a slower real-model call behind a mock for the demo, this is the actual system."

## Step 2 — Watch the graph build itself

You land on the **Execution Graph** tab automatically. Narrate as it grows:

- "One node — Business Analysis — that's all the Supervisor commits to up front."
- (a few seconds later) "Now it's expanded: Project Planning, then Architecture Design."
- (Backend + Frontend appear together) "And here — two nodes in the same column. The Supervisor
  determined these don't depend on each other, so they're dispatched in parallel. That's not a
  hardcoded template, that's read off the actual dependency graph."

## Step 3 — Prove the reasoning is real

Click the **BackendImplementation** node once it's green. Inspector panel opens.

> "Twelve reasoning stages, every single one timed and persisted. Observe, Understand, Think, Plan,
> retrieve context, retrieve memory, select tools, execute, reflect, self-critique, evaluate
> confidence, publish. This same twelve-stage pipeline runs for every agent, every task — that
> uniformity is a deliberate architectural choice, not an accident of how I happened to build the
> first agent."

Point at the confidence percentage: "That's the literal output of the ConfidenceEvaluation stage —
click through and you'll see it in the trace list."

## Step 4 — Prove it's not a recording (Playback)

Switch to the **Playback** tab, hit play.

> "This graph is rebuilding itself from real checkpoint history — the scheduler snapshots the
> entire DAG state after every single scheduling pass. What you're watching is nine real historical
> states, not an interpolation between the start and the end."

## Step 5 — Supervisor accountability

Switch to **Supervisor** tab.

> "Every decision the Supervisor made — to expand the graph this way, to dispatch these two tasks
> together — is logged with a rationale and a confidence score. This is the audit trail; nothing
> the Supervisor does is a black box."

## Step 6 — Artifacts, and the honesty point

Switch to **Artifacts** tab, open one.

> "Every one of these is what the agent actually produced." (open the Artifacts Explorer for a
> Markdown + a code artifact) "Rendered Markdown for docs, Monaco with syntax highlighting for
> code, and if there were a second version, a real diff view — not a screenshot of one, an actual
> Monaco diff editor."

Navigate to **Project Health**.

> "And here's the part I'd actually point to as the most telling design decision in this whole
> project: these two categories — Security, Maintainability — are shown as unmeasured, not given a
> fake score. There's no static-analysis subsystem built yet. Every other score on this page has
> its exact formula on hover. I'd rather show you a gray ring than a number I made up."

## Closing line

> "Everything you just watched — the graph, the reasoning, the supervisor's decisions, the
> artifacts — is real execution data from an actual multi-agent run that happened in the last
> ninety seconds. Nothing here is a mockup."

## Fallback if live demo isn't possible

Use [docs/video/mission-control-demo.webm](../video/mission-control-demo.webm) and narrate the same
beats against the recording.
