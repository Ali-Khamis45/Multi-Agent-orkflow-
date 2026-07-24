# Performance Baseline (Phase 1.5 §12)

Measured with `ai-runtime/scripts/benchmark.py` against a full `docker compose
up` stack (Postgres 16, Redis 7, the .NET API, and the AI Runtime, all on one
Docker host) running the standard success-criteria request ("Build a Task
Management SaaS") end-to-end, plus direct API timing for the two isolated
operations. These are **initial baseline numbers on one dev machine's Docker
Desktop, single-run, mock model provider (no live LLM latency)** — meant as a
reference point to catch regressions against, not a production SLA.

| Metric | Value | Notes |
|---|---|---|
| Workflow startup latency (`POST /intake` response) | **77ms** | Time to create the workspace/workflow/BA node and start the run — returns before any agent executes. |
| Total pipeline completion time | **1.54s** | Full 7-node DAG (BA → PM → Architect → {Backend, Frontend} → Review → QA), all 12 reasoning stages × 7 agents, mock provider. |
| Average reasoning stage latency | **1.75ms** (84 stages) | See per-stage breakdown below. |
| Average artifact creation time | **45.3ms** (n=5) | `POST /api/artifacts`, 500-byte content, cold requests. |
| Average memory lookup time | **2.65ms** (n=5) | `GET /api/memory`, single scope, 20-row limit. |
| Parallel dispatch skew (Backend vs. Frontend) | **2.1ms** | Time between the two parallel nodes' first traced stage (`Observe`) — both were dispatched in the same scheduling pass (§5.2 step 3). |

## Per-stage reasoning latency (this run)

| Stage | Avg duration | n |
|---|---|---|
| Observe | 0.0ms | 7 |
| Understand | 0.0ms | 7 |
| Think | 0.0ms | 7 |
| Plan | 0.0ms | 7 |
| RetrieveContext | 3.4ms | 7 |
| RetrieveMemory | 6.1ms | 7 |
| SelectTools | 0.0ms | 7 |
| Execute | 10.1ms | 7 |
| Reflect | 0.0ms | 7 |
| SelfCritique | 0.0ms | 7 |
| ConfidenceEvaluation | 0.0ms | 7 |
| PublishResult | 1.3ms | 7 |

`RetrieveContext`/`RetrieveMemory`/`Execute`/`PublishResult` are the only
stages that make a network call (artifact-by-name lookup, memory
recall/write, mock model "call", event publish respectively) — the rest are
pure in-process bookkeeping, which is why they round to 0ms. With a real LLM
provider instead of the mock, `Execute` and `SelfCritique` become the
dominant cost by a wide margin (typically 500ms–5s per call), not shown here.

## What this does *not* yet measure

- **Real Redis event publish-to-consumption latency.** The benchmark
  approximates this via the skew between two parallel nodes' first traced
  stage, which bounds *scheduling* skew but not the raw publish→consume gap
  for a single event. Measuring that precisely needs a `ConsumedAt` timestamp
  recorded by the consumer, compared to the envelope's `Timestamp` — not
  implemented this milestone; a natural addition once OpenTelemetry spans
  (§1) are wired up.
- **Real LLM provider latency/cost.** This run used the Multi-Model Router's
  mock fallback (§E7) — no network egress to Anthropic/OpenAI/Gemini/Ollama.
  Re-run `scripts/benchmark.py` with a provider key configured to get
  realistic Execute/SelfCritique timings and non-zero cost estimates.
- **Load/concurrency behavior.** This is a single sequential request against
  an otherwise idle stack — no measurement yet of throughput under multiple
  concurrent workflow runs, or of Postgres/Redis contention at scale.

## Reproducing

```bash
docker compose up -d --build
cd ai-runtime
pip install -e .
python scripts/benchmark.py
```
