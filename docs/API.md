# API Reference

Base URL (local): `http://localhost:5080`. All request/response bodies are JSON, **camelCase**
(ASP.NET Core's default MVC formatter) — this is distinct from the Redis `EventEnvelope` and
`Checkpoint.SnapshotJson`, which are raw `System.Text.Json` output and stay **PascalCase**; see
[Event Bus](architecture/EVENT_BUS.md#eventenvelope) if you're consuming those directly instead of
through this API.

## Authentication

**Phase 2 ("AI Enterprise OS") added JWT bearer authentication.** Register or log in via
`api/auth` below to get a token; send it as `Authorization: Bearer <token>` on every endpoint
marked 🔒 in the sections below. Endpoints with no lock are either intentionally public
(`api/auth/register`, `api/auth/login`) or server-to-server only (see each section).

A user's `CompanyType` (`SoftwareCompany` or `Founder`) is fixed at registration and embedded in
the token as a `company_type` claim — every CompanyType-scoped endpoint (Registry, Intake) reads
it from there, never from the request body, so a caller can never route a request into a company
they don't belong to. See [Architecture Overview](architecture/OVERVIEW.md#phase-2--ai-enterprise-os-workspaces).

This supersedes the "no authentication" gap noted in
[Security Review §1](reviews/SECURITY_REVIEW.md#1-authentication--authorization) for Release 1.0 —
that review predates Phase 2 and describes the Release 1.0 snapshot, not current behavior.

## Errors

**`GlobalExceptionMiddleware`** now maps exceptions to a consistent JSON error shape across every
endpoint:

| Exception | Status |
|---|---|
| `KeyNotFoundException` | `404 Not Found` |
| `UnauthorizedAccessException` (e.g. bad login credentials) | `401 Unauthorized` |
| `ConflictException` (e.g. duplicate email on register) | `409 Conflict` |
| anything else | `500` (logged server-side with the request's correlation id) |

```json
{ "title": "Not Found", "status": 404, "detail": "..." }
```

FluentValidation failures (for commands with a validator registered) still surface separately as
`400 Bad Request`:
```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "rawInput": ["RawInput must not be empty."] }
}
```

This supersedes the "bare 500, no structured body" gap noted in
[Code Review §1](reviews/CODE_REVIEW.md#high-severity-no-global-exception-handling-middleware) for
Release 1.0 — that review predates Phase 2.

---

## Auth — `api/auth`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/auth/register` | `{ "email", "password", "name", "companyType": "SoftwareCompany" \| "Founder" }` | `{ userId, email, name, companyType, token }` |
| `POST` | `/api/auth/login` | `{ "email", "password" }` | `{ userId, email, name, companyType, token }` |
| 🔒 `GET` | `/api/auth/me` | — | `{ userId, email, name, companyType }` |

Registration also creates a `Workspace` named `"default"` owned by the new user in the same
transaction — every user has exactly one workspace to start with. `companyType` is permanent —
there is no endpoint to change it after registration.

```bash
curl -X POST http://localhost:5080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"a@b.com","password":"password123","name":"Ada","companyType":"Founder"}'
# → {"userId":"...","email":"a@b.com","name":"Ada","companyType":"Founder","token":"eyJ..."}
```

## Company Profile — `api/company-profile` (Phase 3, Founder Workspace only)

Company Memory's source of truth (see [Architecture Overview](architecture/OVERVIEW.md#phase-3--ai-company-operating-system-company-memory)).
Dual-auth like `POST /api/workspaces`: a valid JWT (the frontend) or `X-Internal-Service-Key` (the
AI Runtime's Founder agents, reading/writing with no user session).

| Method | Path | Body | Returns |
|---|---|---|---|
| `GET` | `/api/company-profile?workspaceId=` | — | `{ id, workspaceId, isOnboarded, profileJson, updatedAt }` — get-or-create, `profileJson` is a JSON *string* to parse client-side |
| `PATCH` | `/api/company-profile/section` | `{ workspaceId, section, patch }` | same shape — field-level merge-patch of one section; fields not in `patch` are untouched |
| `POST` | `/api/company-profile/onboarding/complete` | `{ workspaceId, profile }` | same shape, `isOnboarded: true` — replaces the whole profile, merged onto the default shape |
| `GET` | `/api/company-profile/health?workspaceId=` | — | `{ overallScore, categories: [{ category, score, present, missing, explanation }] }` |
| `GET` | `/api/company-profile/timeline?workspaceId=` | — | `{ title, artifactName, at, ownerAgent }[]` — real artifact-creation milestones, oldest first |
| `GET` | `/api/company-profile/recommendations?workspaceId=&limit=` | — | `{ category, text, categoryScore }[]` — restates the lowest-scoring Business Health gaps |

`section` is one of `basicInfo | brand | products | customers | business | competition |
marketing | operations` — see `CompanyProfileJson.DefaultProfileJson` in
`api/Domain/Founders/CompanyProfileJson.cs` for the full field shape of each.

```bash
curl "http://localhost:5080/api/company-profile?workspaceId=<id>" -H "Authorization: Bearer $TOKEN"
# → {"id":"...","workspaceId":"...","isOnboarded":true,"profileJson":"{\"basicInfo\":{...},...}","updatedAt":"..."}
```

## Connectors — `api/connectors` (Phase 4)

Reaches real external systems (Shopify, GitHub, Stripe, ...) — see
[Architecture Overview](architecture/OVERVIEW.md#phase-4--connector-framework-real-business-systems).
`installed`/`health`/`sync`/`actions` are dual-auth (JWT or `X-Internal-Service-Key`, same as
`api/company-profile`) since the AI Runtime's `connector_action` tool calls them with no user
session. `oauth/callback` has no auth at all — it's a browser redirect landing straight from the
third-party provider; its security is the signed `state` param, not a bearer token.

| Method | Path | Body | Returns |
|---|---|---|---|
| `GET` | `/api/connectors/catalog` | — | `{ key, displayName, description, authType, oAuthAvailable, requiredCredentialFields, actions, events }[]` — scoped to the caller's own CompanyType |
| `GET` | `/api/connectors/installed?workspaceId=` | — | `{ connectorKey, displayName, status, lastHealthCheckAt, lastHealthOk, lastHealthMessage, lastSyncedAt, lastSyncOk, lastSyncMessage }[]` |
| `POST` | `/api/connectors/{key}/install` | `{ workspaceId, credentials: { [field]: string } }` | `204` — `credentials` keys must match that connector's `requiredCredentialFields` for API-key connectors; for OAuth2 connectors this is also how `CompleteConnectorOAuthCommand` persists an exchanged token |
| `POST` | `/api/connectors/{key}/disconnect` | `{ workspaceId }` | `204` |
| `GET` | `/api/connectors/{key}/oauth/authorize-url?workspaceId=` | — | `{ url }` — the real provider authorize URL, `state` HMAC-signed to this workspace+connector |
| `GET` | `/api/connectors/{key}/oauth/callback?code=&state=` | — | `302` redirect back to the frontend marketplace page |
| `POST` | `/api/connectors/{key}/health` | `{ workspaceId }` | `{ healthy, message }` |
| `POST` | `/api/connectors/{key}/sync` | `{ workspaceId }` | `{ success, summary }` — on success, also applies the result to CompanyProfile (Founder) or Memory (Software) |
| `POST` | `/api/connectors/{key}/actions/{actionKey}` | `{ workspaceId, inputJson }` | `{ success, outputJson, errorMessage }` — every call, success or failure, is logged to `connector_action_logs` |

```bash
curl -X POST http://localhost:5080/api/connectors/shopify/install \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"workspaceId":"<id>","credentials":{"storeUrl":"your-store.myshopify.com","accessToken":"shpat_..."}}'
curl -X POST http://localhost:5080/api/connectors/shopify/sync \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d '{"workspaceId":"<id>"}'
```

## Workspaces — `api/workspaces`

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/workspaces` | `{ "name": string }` | `Guid` (new workspace id) — accepts either a valid JWT (owner = caller) or an `X-Internal-Service-Key` header matching the server's configured key (owner = none, used by the AI Runtime's own startup bootstrap) |
| 🔒 `GET` | `/api/workspaces` | — | `Workspace[]` — `{ id, name, createdAt }`, scoped to the caller's own workspaces, newest first |

## Intake — `api/intake`

The only endpoint that starts new work, and one of two that proxy server-to-server to the AI
runtime (see [Architecture Overview](architecture/OVERVIEW.md#system-design)).

| Method | Path | Body | Returns |
|---|---|---|---|
| 🔒 `POST` | `/api/intake` | `{ "rawInput": string, "workspaceId"?: Guid }` | `{ "workflowRunId": Guid }` |

`companyType` is **not** a request field — it's read from the caller's JWT and determines which
fixed Supervisor pipeline runs (Software Company's 7-node pipeline, or Founder's 11-node pipeline;
see [Supervisor Brain](architecture/SUPERVISOR_BRAIN.md)).

```bash
TOKEN=$(curl -s -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" -d '{"email":"a@b.com","password":"password123"}' \
  | jq -r .token)
curl -X POST http://localhost:5080/api/intake \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
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

`POST`/`PUT` are server-to-server (agent self-registration/heartbeat on startup, before any user
session exists) and stay unauthenticated. `GET` is scoped to the caller's own `CompanyType` — a
Software Company user sees the 7 Software agents; a Founder user sees the 11 `founder-*` agents;
neither ever sees the other's.

| Method | Path | Body | Returns |
|---|---|---|---|
| `POST` | `/api/registry/agents` | agent registration payload (includes `companyType`) | — |
| `PUT` | `/api/registry/agents/{name}/heartbeat?companyType=` | — | — |
| 🔒 `GET` | `/api/registry/agents` | — | `AgentDto[]` — `{ name, version, description, skills, supportedTasks, priority, status, inFlightTaskCount, lastHeartbeatAt, companyType }` |

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
