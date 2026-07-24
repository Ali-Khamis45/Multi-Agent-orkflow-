# AI Agents Team

Autonomous AI software engineering company: a supervisor coordinates a dynamic
registry of agents through a DAG-scheduled workflow, communicating via an
event bus, with human approval gates at key stages.

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full design and phased build
order.

**Stack**: ASP.NET Core 10 (orchestration) · Python/FastAPI (AI runtime) ·
Next.js 16/React 19 (frontend) · PostgreSQL + pgvector · Redis · Docker.
