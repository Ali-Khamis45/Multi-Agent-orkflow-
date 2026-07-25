# API Reference

Base URL (local): `http://localhost:5080`. All request/response bodies are JSON, **camelCase**
(ASP.NET Core's default MVC formatter) — this is distinct from the Redis `EventEnvelope` and
`Checkpoint.SnapshotJson`, which are raw `System.Text.Json` output and stay **PascalCase**; see
[Event Bus](architecture/EVENT_BUS.md#eventenvelope) if you're consuming those directly instead of
through this API.

## Authentication

**None.** Every endpoint below is open to any caller who can reach the port. This is a deliberate,
documented scope boundary for this release — see [Security Review §1](reviews/SECURITY_REVIEW.md#1-authentication--authorization)
and [Deployment](DEPLOYMENT.md#before-deploying-anywhere-beyond-localhost) before exposing this API
beyond localhost.

## Errors

Two shapes exist today (see [Code Review §1](reviews/CODE_REVIEW.md#high-severity-no-global-exception-handling-middleware)
for the gap this implies):

**Validation failures** (FluentValidation, for the ~7 commands that have a validator registered) —
`400 Bad Request`:
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "rawInput": ["RawInput must not be empty."] }
}
```

**Missing-resource failures** (`KeyNotFoundException`, thrown by most handlers when an id doesn't
resolve) currently surface as a **bare 500** with no structured body — not yet mapped to a 404. If
you're integrating against this API today, treat any 500 on a lookup-by-id call as "probably not
found" until this is fixed (tracked in the [Roadmap](ROADMAP.md)).

A handful of query endpoints (e.g. `GET /api/workflows/runs/{id}`) do check for `null` explicitly
and return a proper `404 Not Found` — inconsistent with the above until the global handler lands.

---

## Workspaces — `api/workspaces`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/workspaces` | `{ "name": string }` | `Guid` (new workspace id) |
| `GET` | `/api/workspaces` | — | `Workspace[]` — `{ id, name, createdAt }`, newest first |

## Intake — `api/intake`

The only endpoint that starts new work, and one of two that proxy server-to-server to the AI
runtime (see [Architecture Overview](architecture/OVERVIEW.md#system-design)).

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/intake` | `{ "rawInput": string, "workspaceId"?: Guid }` | `{ "workflowRunId": Guid }` |

```bash
curl -X POST http://localhost:5080/api/intake \
  -H "Content-Type: application/json" \
  -d '{"rawInput":"Build a Task Management SaaS","workspaceId":"<workspace-guid>"}'
# → {"workflowRunId":"2962bc19-..."}
```

⚠️ No length validation on `rawInput` today — see [Security Review §5](reviews/SECURITY_REVIEW.md#5-input-validation).

## Intent — `api/intent`

Lower-level intent-analysis primitives; `POST /api/intake` above is the composed happy path most
callers want.

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/intent/sessions` | — | `Guid` (new session id) |
| `POST` | `/api/intent/sessions/{id}/analysis` | intent analysis payload | — |
| `POST` | `/api/intent/sessions/{id}/answers` | clarification answer payload | — |
| `POST` | `/api/intent/sessions/{id}/structure` | — | — |
| `GET` | `/api/intent/sessions/{id}` | — | `IntentSessionDto` |

## Workflows — `api/workflows`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/workflows/runs` | create-run payload | `Guid` |
| `POST` | `/api/workflows/runs/{runId}/nodes` | task-node payload | `Guid` |
| `POST` | `/api/workflows/runs/{runId}/edges` | `{ predecessorNodeId, successorNodeId }` | — |
| `POST` | `/api/workflows/runs/{runId}/start` | — | — |
| `POST` | `/api/workflows/runs/{runId}/reschedule` | — | — |
| `GET` | `/api/workflows/runs/{runId}` | — | `WorkflowRunDto` (full, with nodes + edges) |
| `GET` | `/api/workflows/runs?workspaceId=&status=&limit=` | — | `WorkflowRunDto[]` |

```json
// GET /api/workflows/runs/{id} →
{
  "id": "2962bc19-...", "workspaceId": "...", "correlationId": "...",
  "goal": "Build a Task Management SaaS", "status": "Completed",
  "createdAt": "2026-07-24T23:11:23Z", "updatedAt": "2026-07-24T23:11:24Z",
  "nodes": [
    { "id": "...", "name": "BusinessAnalysis", "taskType": "DiscoverRequirements",
      "status": "Completed", "assignedAgentName": "business-analyst",
      "confidence": 0.85, "riskLevel": null, "attemptCount": 1,
      "createdAt": "...", "updatedAt": "..." }
  ],
  "edges": [{ "predecessorNodeId": "...", "successorNodeId": "..." }]
}
```

`status` is one of `Planning | Running | WaitingApproval | Paused | Completed | Failed | RolledBack`.
Node `status` is one of `Pending | Ready | Dispatched | Running | Completed | Failed | Blocked |
WaitingApproval`.

## Checkpoints — `api/checkpoints`

Powers [Execution Playback](architecture/WORKFLOW_ENGINE.md#checkpoints--execution-playback).

| Method | Path | Body | Returns |
|---|---|---|---|
| `GET` | `/api/checkpoints?workflowRunId=` | — | `CheckpointDto[]`, oldest first — `{ id, workflowRunId, label, snapshotJson, createdAt }` |

`snapshotJson` is a **PascalCase** raw-serialized string (see the note at the top of this document)
— parse it separately from the rest of the (camelCase) response.

## Registry — `api/registry`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/registry/agents` | agent registration payload | — |
| `PUT` | `/api/registry/agents/{name}/heartbeat` | — | — |
| `GET` | `/api/registry/agents` | — | `AgentDto[]` — `{ name, version, description, skills, supportedTasks, priority, status, inFlightTaskCount, lastHeartbeatAt }` |

## Supervisor — `api/supervisor`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/supervisor/decisions` | decision payload | `Guid` |
| `GET` | `/api/supervisor/decisions?workflowRunId=` | — | `SupervisorDecisionDto[]`, oldest first, one run |
| `GET` | `/api/supervisor/summary?workspaceId=&limit=` | — | `{ counts: [{decisionType, count}], recent: SupervisorDecisionDto[] }`, workspace-wide |

## Reasoning — `api/reasoning`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/reasoning/traces` | trace payload (one per pipeline stage) | `Guid` |
| `GET` | `/api/reasoning/traces/{taskNodeId}` | — | `ReasoningTraceDto[]`, one task node, in stage order |
| `GET` | `/api/reasoning/telemetry?workspaceId=&pointsLimit=` | — | `{ stageMetrics: [...], recentPoints: [...] }`, workspace-wide aggregate |
| `GET` | `/api/reasoning/agents/{agentName}/traces?limit=` | — | `ReasoningTraceDto[]`, cross-workflow, one agent |

`stage` is one of the [12 reasoning stages](architecture/REASONING_ENGINE.md#the-12-stages):
`Observe | Understand | Think | Plan | RetrieveContext | RetrieveMemory | SelectTools | Execute |
Reflect | SelfCritique | ConfidenceEvaluation | PublishResult`.

## Observability — `api/observability`

| Method | Path | Body | Returns |
|---|---|---|---|
| `GET` | `/api/observability/agents/metrics?agentName=` | — | `AgentMetricsDto[]` — success/failure rate, avg confidence, avg stage duration, tool/memory counts, model usage |
| `GET` | `/api/observability/agents/{agentName}/confidence-trend?limit=` | — | `ConfidencePointDto[]` — `{ at, confidence }` |

## Artifacts — `api/artifacts`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/artifacts` | artifact payload (idempotency-key aware — see [Workflow Engine](architecture/WORKFLOW_ENGINE.md#idempotency)) | `Guid` |
| `GET` | `/api/artifacts/{id}` | — | `ArtifactDto` |
| `GET` | `/api/artifacts/{id}/versions` | — | `ArtifactDto[]`, newest first, full version chain |
| `GET` | `/api/artifacts/by-name?workflowRunId=&name=` | — | `ArtifactDto` (latest version) — used internally by join nodes, see [Execution Flow](architecture/EXECUTION_FLOW.md#the-dag) |
| `GET` | `/api/artifacts?workspaceId=&workflowRunId=&type=&search=&limit=` | — | `ArtifactDto[]` — latest version of every logical artifact matching the filter |

`type` is one of `Code | Markdown | Json | Test | Dockerfile | Sql | Image | Diagram`.

## Memory — `api/memory`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/memory` | `{ workspaceId, layer, scopeRef, kind, content, sourceArtifactId?, ttlAt?, correlationId? }` | `Guid` |
| `GET` | `/api/memory?workspaceId=&layer=&scopeRef=&limit=` | — | `MemoryItemDto[]`, one exact scope |
| `GET` | `/api/memory/overview?workspaceId=&layer=&limit=` | — | `{ layerCounts: [...], items: [...] }`, browsable across every scope |

`layer` is one of `Working | Conversation | Workflow | Project | LongTerm` — see
[Memory](architecture/MEMORY.md#the-five-layers).

## Prompts — `api/prompts`

The second server-to-server proxy endpoint (see [Architecture Overview](architecture/OVERVIEW.md#system-design)).

| Method | Path | Body | Returns |
|---|---|---|---|
| `GET` | `/api/prompts` | — | `PromptEntryDto[]` — `{ name, owner, compatibleAgent, currentVersion, versions: [{version, file, description, variables, createdAt}] }` |

---

## SignalR — `/hubs/workflow`

Not a REST endpoint — a persistent connection (`@microsoft/signalr` on the frontend). No
authentication (see [Security Review §8](reviews/SECURITY_REVIEW.md#8-signalr)).

| Client → Hub | Params | Effect |
|---|---|---|
| `JoinWorkflow` | `workflowRunId: string` | Subscribe to that run's live events |
| `LeaveWorkflow` | `workflowRunId: string` | Unsubscribe |

| Hub → Client | Payload |
|---|---|
| `workflowEvent` | `{ type, taskId, producedBy, timestamp, confidence, riskLevel }` — see [Event Bus](architecture/EVENT_BUS.md#eventenvelope) |

The payload is intentionally a subset of the full `EventEnvelope` — the client always re-fetches the
real row (via the REST endpoints above) rather than trusting the push payload as a source of truth;
the event is a "something changed, go refetch" signal, not the data itself.
