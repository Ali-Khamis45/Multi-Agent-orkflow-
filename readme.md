# AI Agents Team

Autonomous AI software engineering company: a supervisor coordinates a dynamic
registry of agents through a DAG-scheduled workflow, communicating via an
event bus, with human approval gates at key stages.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full design and phased build
order, and [ARCHITECTURE_EXTENSION.md](ARCHITECTURE_EXTENSION.md) for the
enterprise-grade extension layer (Supervisor Brain, Intent Engine,
Hierarchical Planner, Reasoning Engine, Multi-Model Router, Learning Engine,
Governance, Marketplace, and more) built additively on top of it.

Phase 1 (the working end-to-end prototype) is implemented in `api/` (.NET
orchestration service) and `ai-runtime/` (Python AI runtime). See
[PHASE_1_5_HARDENING.md](PHASE_1_5_HARDENING.md) for the production-hardening
pass (telemetry, correlation IDs, idempotency, structured errors, execution
snapshots, agent metrics, sandboxing, prompt registry, validation,
integration tests) and [PERFORMANCE_BASELINE.md](PERFORMANCE_BASELINE.md) for
initial measured performance numbers.

**Stack**: ASP.NET Core 10 (orchestration) · Python/FastAPI (AI runtime) ·
Next.js 16/React 19 (frontend) · PostgreSQL + pgvector · Redis · Docker.
