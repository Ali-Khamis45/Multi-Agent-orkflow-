# Technical Retrospective — Release 1.0

A short, honest look back across the whole build: architecture, working prototype, hardening,
Mission Control, and this release-engineering pass.

## What went well

- **The architectural boundaries held, and stayed checkable.** "The frontend never talks to Python"
  and "the AI runtime never touches the database" were stated on day one and were still literally
  true at release — verified by grep, not just by memory, in the Code Review. That's a rarer
  outcome than it sounds; boundaries like this usually erode under deadline pressure.
- **Uniformity paid for itself later.** Every agent running through the identical 12-stage reasoning
  pipeline was a Phase 1 decision made for consistency's sake. By Phase 1.6 it meant one Reasoning
  Inspector component could render *any* agent's work with zero agent-specific logic, and the
  Telemetry Center's per-stage charts were apples-to-apples across all seven agents for free.
- **A feature built for one reason turned out to be exactly what a much better feature needed.**
  `Checkpoint` snapshots were built in Phase 1.5 for a future resume/replay/debugging story. When
  Mission Control needed Execution Playback, the checkpoints were already there, already correct,
  and already had everything needed — no redesign, just a new read path.
- **The mock-provider-first design made every later verification cheap.** Because the whole system
  runs end-to-end on a deterministic mock model with zero API keys, every screenshot, the demo
  video, and every "does this actually work" check in this project could be re-run in under a
  minute, as many times as needed, for free.
- **The "don't fake it" discipline held under real pressure to look complete.** Project Health
  shows two categories as explicitly unmeasured rather than a plausible-looking invented score;
  the Memory Inspector labels Vector Memory and Knowledge Graph as "planned, not built" instead of
  rendering an empty panel that implies something's wrong. Easy to compromise on when you want a
  demo to look finished — didn't happen here.

## Biggest architectural decisions

1. **Three services, not one.** Splitting orchestration (.NET), reasoning (Python), and
   presentation (Next.js) into separately-deployable services with an enforced, one-directional
   dependency shape — rather than a monolith or a thinner two-service split — was the highest-
   leverage decision in the whole build. It's what made "the AI runtime never touches the
   database" a real, verifiable constraint instead of a suggestion.
2. **Redis Streams with independent consumer groups**, not a single queue. `orchestrator`,
   `signalr-relay`, and `ai-runtime-agents` all read the same event stream independently, so a slow
   or crashed dashboard relay can never block scheduling, and a crashed agent process can never
   lose an event. This is more infrastructure than a smaller project would reach for — it paid off
   directly in the Performance Review finding nothing to flag in this subsystem at all.
3. **A dynamically-expanded DAG instead of a workflow template.** The Supervisor Brain builds the
   task graph one decision at a time as work completes, rather than instantiating a fixed pipeline
   template. This is *why* the Execution Graph, Playback, and Supervisor Brain pages are
   interesting to look at instead of a progress bar — the system is visibly deciding, not just
   executing a script.
4. **Checkpoint-based state over event-sourcing.** Full-state snapshots after every scheduling pass,
   not a replay-from-events model. Simpler to implement and reason about, and it's what made
   Playback buildable in an afternoon instead of a redesign.

## Biggest technical challenges

- **Next.js 16 / React 19 were genuinely bleeding-edge** during this build — recent enough that
  assumptions from training data were sometimes wrong. The project's own `frontend/AGENTS.md`
  instruction to read the bundled framework docs before writing App Router code wasn't
  ceremonial; it caught real API differences (the `PageProps`/`LayoutProps` global helper types,
  `params` as a `Promise`) before they became bugs.
- **Base UI's API surface looks like Radix's but isn't**, and that gap caused the two hardest bugs
  to find in this entire project (see below) — both because they *compiled cleanly* and produced
  no error, just silently did nothing.
- **Hand-rolling the DAG layout.** No layout library was in the mandated stack, so the layered
  longest-path column algorithm that makes parallel branches (Backend + Frontend) render in the
  same column had to be designed from scratch. Small in code size, easy to get subtly wrong.
- **Keeping live SignalR pushes and TanStack Query's cache in sync without races** — the eventual
  pattern (push events carry only enough to know *what* changed, the client always refetches the
  real row) took a couple of iterations to land on, but held up cleanly once it did.

## Bugs discovered during implementation

Roughly chronological, across the whole project — the ones worth remembering:

| Bug | Phase | Why it mattered |
|---|---|---|
| EF Core silently not detecting new entities in field-backed collections | Phase 1 | Required explicit `db.Set.Add()` alongside domain methods — a real EF Core gotcha, not a logic error |
| JSON enums serialized as integers, causing 400s | Phase 1 | Fixed globally via `JsonStringEnumConverter`, not per-endpoint |
| A broken LINQ `ValueTuple` double-projection silently failing every reasoning-trace write | Phase 1.5 | Silent failure — no exception, just missing data. The dangerous kind |
| `suggestedAction` snake_case/camelCase mismatch between Python and .NET | Phase 1.5 | The first hint of what would later be a documented, explicit convention (see API.md) |
| CORS allowing only port 3000 while the dev server landed on 3001 | Phase 1.6 | Blocked every dashboard request silently — caught immediately by *actually opening the browser*, not by reading code |
| `cmdk`'s `CommandDialog` never wrapping children in the real `Command` root | Phase 1.6 | Crashed Ctrl+K outright the first time it was exercised live |
| **Base UI's `Menu.Item` has no `onSelect` prop** — it's silently accepted as a no-op because React treats `onSelect` as an unrelated, generic native DOM attribute | Phase 1.6 | **The big one.** This meant the workspace switcher never actually worked, for an entire session, despite looking correct in every prior screenshot — nothing crashed, nothing errored, it just silently did nothing. Found by accident, while testing an unrelated export-menu download that used the same broken pattern. Fixed everywhere it appeared once found. |
| `DropdownMenuLabel` requiring a `DropdownMenuGroup` wrapper (a Base UI requirement Radix doesn't have) | Phase 1.6 | Crashed the export menu outright on first use |
| `setState`-in-effect anti-pattern in the Artifacts Explorer | Release 1.0 | Only surfaced when `npm run lint` was finally run during final validation — it had never been run before that point |
| Two documentation inaccuracies (wrong endpoint count, a GitHub-blob-relative link that only worked in one rendering context) | Release 1.0 | Caught only by an independent final review that re-executed checks instead of trusting the documents as written |

## Lessons learned

1. **Compiling clean and working are different claims.** Every bug in the table above except the
   first two compiled without error and produced no exception — they were silent. The pattern that
   actually catches this class of bug is *running the thing and looking at it* (or running the real
   test/lint/build command), not reading the code more carefully. That habit — screenshot every UI
   milestone, run the actual lint/test commands before calling something done — was adopted
   partway through this project and every bug found from that point forward came from it, not from
   review.
2. **A library that mimics a familiar API is more dangerous than one that looks unfamiliar.** Base
   UI's `render` prop and `onClick` handler *look* like Radix's `asChild` and `onSelect` closely
   enough that muscle memory from one bled into the other, repeatedly, across this project. The fix
   isn't "be more careful" — it's checking the actual type definitions the first time a new
   component from an unfamiliar-but-similar library gets used.
3. **A fresh clone finds what a warm working directory hides.** The final review's "clone into an
   isolated directory and build there" step wasn't ceremonial — it's the only way to be sure the
   README is actually complete, rather than complete-plus-whatever-state-was-already-on-disk.
4. **Infrastructure built for one reason is worth over-building slightly, because you often don't
   know the second reason yet.** Checkpoints weren't built for Playback. Reasoning traces weren't
   built for a Telemetry dashboard. Both existed for their own Phase 1.5 justification, and both
   turned out to be exactly the right shape for a Phase 1.6 feature nobody had designed yet.
5. **Saying "unmeasured" costs less credibility than it feels like it will.** The instinct when
   building something that's supposed to look impressive is to fill every gap. This project's
   Project Health page and Memory Inspector both resisted that instinct, and the resulting product
   reads as more trustworthy for it, not less finished.

## Recommended priorities for Phase 2

In order, matching [docs/ROADMAP.md](ROADMAP.md)'s "Immediate" section:

1. **Authentication and authorization.** The single largest gap, called out repeatedly and
   deliberately rather than hidden. Nothing else on this list matters if this system is ever
   reachable by anyone other than its own operator.
2. **Global exception-handling middleware in the API**, closing the `KeyNotFoundException` → bare
   500 gap found in the Code Review. Small, mechanical, and removes the single most likely source
   of a confusing bug report from a new contributor.
3. **Frontend test coverage**, starting with the untested pure logic (`lib/health-score.ts`,
   `lib/dag-layout.ts`, `lib/export.ts`) rather than trying to cover the whole component tree at
   once.
4. **Fix `GetArtifactVersionsQuery`'s full-table load** — cheap fix, real scaling concern, already
   has the index it needs.
5. **After that**, the next genuinely new capability worth building (rather than hardening) is
   likely **Vector Memory** — the schema was deliberately built to support it without a migration,
   and it's the extension subsystem where "we already built the hard part, we just haven't turned
   it on" is most true.
