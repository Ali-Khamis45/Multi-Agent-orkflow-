# FAQ

**Do I need an API key to try this?**
No. The Multi-Model Router falls back to a deterministic mock provider when no
`ANTHROPIC_API_KEY`/`OPENAI_API_KEY`/`GEMINI_API_KEY`/`OLLAMA_HOST` is set, and the whole pipeline —
intent analysis, all 7 agents, all 12 reasoning stages, the Supervisor Brain — runs end-to-end on
it. Every screenshot and the recorded walkthrough in this repo were produced entirely on the mock.

**How do I see it work fastest?**
`docker compose up -d`, then `cd frontend && npm install && npm run dev`, open
`http://localhost:3000`, and click **Run demo** on the Dashboard's Portfolio Demo banner. It
submits "Build a Task Management SaaS" to the real pipeline and follows it live. See the
[Recruiter Quickstart](demo/RECRUITER_QUICKSTART.md) for a guided version.

**Is this production-ready?**
For a single-operator, local/demo deployment — yes, that's exactly its current scope, and it's
validated as such (see the [Release Checklist](RELEASE_CHECKLIST.md)). For anything exposed beyond
localhost — **not yet**: there is no authentication anywhere in the system today. This is called
out explicitly, not discovered late, in the [Security Review](reviews/SECURITY_REVIEW.md) and
[Deployment guide](DEPLOYMENT.md#before-deploying-anywhere-beyond-localhost).

**Why does the dashboard talk to the .NET API instead of the Python AI runtime directly?**
By design. The .NET API is the only system of record and the only thing anything else is allowed
to persist through — see [Architecture Overview](architecture/OVERVIEW.md#why-this-shape). Two
endpoints (`/api/intake`, `/api/prompts`) exist purely to proxy the two things the dashboard needs
from the AI runtime, server-to-server, so the browser never has a direct line to it. This boundary
is checked, not just documented — see the [Code Review](reviews/CODE_REVIEW.md).

**Why three services instead of one?**
Each has a genuinely different job and a different natural language/runtime for it: .NET for
durable orchestration and a typed API surface, Python for the LLM-adjacent reasoning/agent
ecosystem (where most of that tooling actually lives), and Next.js for the dashboard. The
[Overall System diagram](architecture/OVERVIEW.md#overall-system-diagram) shows exactly how they're
wired.

**What happens if a task fails?**
Its failure is classified into a `StructuredFailure` (category, severity, retryable or not) rather
than propagating a raw exception. A retryable failure triggers a Supervisor `Retry` decision and
re-dispatch; a terminal one marks the task, and potentially the run, `Failed`. See
[Agent Lifecycle §Retries](architecture/AGENT_LIFECYCLE.md#retries).

**How does "Execution Playback" actually work — is it a recording?**
No — it's real historical state. The scheduler writes a full DAG snapshot (`Checkpoint`) after
every scheduling pass; Playback scrubs through the actual sequence of those snapshots, so the graph
at checkpoint 1 of 9 genuinely only has one node in it. See
[Workflow Engine §Checkpoints](architecture/WORKFLOW_ENGINE.md#checkpoints--execution-playback).

**Why is Security/Maintainability shown as "not measured" on the Project Health page instead of a
score?**
Because there's no real signal to compute one from — no static-analysis or vulnerability-scanning
subsystem exists yet (see [Roadmap](ROADMAP.md), subsystem E15). Every other score on that page is
computed from real execution data with its exact formula shown on hover; inventing a number for
these two would have broken that promise silently.

**Can I add a new agent?**
Yes — an agent is a small declarative subclass of `AgentBase` (name, skills, supported task types,
priority, and one `execute_domain_logic` method). See
[Development Guide §Project conventions](DEVELOPMENT.md#project-conventions) and any of the 7
existing agents in `ai-runtime/app/agents/` as a template.

**Can I add a new dashboard page?**
Yes — follow the existing pattern: a `hooks/queries.ts` hook wrapping `lib/api-client.ts`, a
feature folder under `components/`, and a route under `app/`. If the page needs data the API
doesn't expose yet, add a new query/endpoint following the CQRS convention in
[Development Guide §Project conventions](DEVELOPMENT.md#project-conventions) — most of Phase 1.6's
dashboard pages required exactly one small, additive backend query.

**Where do I report a bug or security issue?**
Functional bugs: [open an issue](../.github/ISSUE_TEMPLATE/bug_report.md). Anything
security-sensitive: see [SECURITY.md](../SECURITY.md) — please don't file those as a public issue.

**What license is this under?**
[MIT](../LICENSE).
