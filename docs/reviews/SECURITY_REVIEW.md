# Security Review — Release 1.0

Scope: `api/`, `ai-runtime/`, `frontend/`, `docker-compose.yml`, dependency manifests. Method:
direct code inspection (no automated SAST tool run), plus `dotnet list package --vulnerable`,
`npm audit`, and `pip-audit` against the declared dependencies.

**Headline finding**: this platform has **no authentication or authorization anywhere** — not in
the API, not on the SignalR hub, not in the frontend. That is an intentional, accepted scope
boundary for a single-tenant local/demo deployment, not an oversight discovered late — but it must
not be missed by anyone deploying this beyond localhost, so it's called out first and loudly here
and in the README/roadmap.

---

## 1. Authentication & Authorization — not implemented

- No `AddAuthentication`/`AddAuthorization` call anywhere in `Api/Program.cs`; no `[Authorize]`
  attribute anywhere in the solution (grepped the full `api/` tree).
- `Program.cs` does call `app.UseAuthorization()`, but with no scheme configured and no
  `[Authorize]` attributes present, it is currently a no-op — every endpoint is open to anyone who
  can reach the port.
- `WorkflowHub` (`Api/Hubs/WorkflowHub.cs`) has no `[Authorize]` and no ownership check: any
  connected client can call `JoinWorkflow(anyGuid)` and receive that run's live event stream. GUIDs
  aren't guessable, but there is no tenant/user concept to check against even if they were known.
- There is no user, workspace-ownership, or role model anywhere in the domain — `Workspace` is a
  bare name + id, not scoped to any principal.

**Impact**: fine for local development and a single-operator demo. **Do not** expose this API,
SignalR hub, or the AI runtime to an untrusted network without adding an auth layer first — there
is currently nothing stopping any caller who can reach the port from reading or writing any
workspace's data.

**Recommendation for post-1.0**: JWT bearer auth on the API (ASP.NET Core has this built in),
`[Authorize]` on `WorkflowHub`, and a `WorkspaceId` ownership check added to every query/command
handler (most already take `WorkspaceId` as a parameter, so this is additive, not a redesign).

## 2. Secrets management

- No secrets are committed. Root `.gitignore` correctly excludes `.env`/`.env.*` while explicitly
  allowlisting `!.env.example`; verified both `ai-runtime/.env.example` and `frontend/.env.example`
  are template files with empty/placeholder values, and no real `.env` file exists on disk or in
  git history for either.
- `appsettings.json`/`appsettings.Development.json` and `docker-compose.yml` contain a **hardcoded
  local development Postgres password** (`Password=aiagentsteam`, matching the also-hardcoded
  Postgres container credentials). This is fine as a zero-config local dev default — the whole
  point of this stack is "clone and `docker compose up`, no setup" — but it must never be reused
  as-is for any non-local deployment. Worth a one-line callout in `DEPLOYMENT.md` (added — see
  Release 1.0 docs) rather than a code change, since changing it would break the zero-config
  promise for local dev.
- The Multi-Model Router's provider keys (`ANTHROPIC_API_KEY`/`OPENAI_API_KEY`/`GEMINI_API_KEY`)
  are read from environment only, never logged, never returned in any API response — verified via
  grep across `ai-runtime/app` for the key names outside `config.py`.

## 3. Docker

- Both `api/Dockerfile` and `ai-runtime/Dockerfile` run as **root** — neither declares a `USER`
  directive or creates a non-root user. Standard oversight, not exploitable on its own, but it
  widens the blast radius of any future RCE-class bug inside either container. Recommendation:
  add a non-root user to both images before 1.0's Docker images are published anywhere public.
- `api/Dockerfile` is a proper multi-stage build (SDK image discarded, only the smaller ASP.NET
  runtime image shipped). `ai-runtime/Dockerfile` is single-stage but has no compiled build step to
  strip, so this is a non-issue there.
- `docker-compose.yml` maps Postgres (5434), Redis (6380), the API (5080), and the AI runtime
  (8000) all directly to the host. Correct and necessary for local development; **if this compose
  file is ever used as a starting point for a non-local deployment**, Postgres and Redis should not
  be published to a public interface at all, and the API/AI-runtime ports should sit behind a
  reverse proxy/firewall. Flagged in `DEPLOYMENT.md`.

## 4. Filesystem sandbox (`ai-runtime`) — solid, verified

`app/tools/sandbox.py`'s `resolve_sandboxed_path` is a genuine defense-in-depth implementation:

1. Rejects absolute input paths outright.
2. Rejects any `..` path segment **before** resolution (not relying solely on post-resolution
   containment).
3. Resolves symlinks (`Path.resolve()`) and re-validates containment against the resolved root
   afterward — this specifically defeats a symlink planted inside the sandbox that points outside
   it, a common way naive sandbox checks get bypassed.
4. Enforces a maximum write size (`FILESYSTEM_MAX_BYTES`, default 1,000,000 bytes) to prevent
   disk-exhaustion via the tool.

`tests/test_sandbox.py` covers traversal, absolute-path, symlink-escape, and oversized-write cases
directly. No escape was found in `FilesystemTool`. This is the strongest-reviewed component in the
codebase — no changes recommended.

## 5. Input validation

- FluentValidation is correctly wired as a MediatR pipeline behavior
  (`ValidationBehavior<TRequest,TResponse>`, registered in `Application/DependencyInjection.cs`),
  and validation failures are mapped to a well-formed 400 response by
  `ValidationExceptionMiddleware`. This part works well where it's used.
- **Coverage gap**: only 7 of the 30 `IRequest` commands/queries in `Application` have a matching
  `AbstractValidator`. The other 23 — including several that accept free-text or unbounded
  collections — pass straight into their handler with no validation at all.
- **`POST /api/intake` has no validation whatsoever.** `IntakeController.Submit` takes a raw
  `SubmitIntakeRequest(string RawInput, Guid? WorkspaceId)` and forwards `RawInput` directly to the
  AI runtime with no length cap, no null/empty check, and no MediatR pipeline in front of it (this
  controller bypasses `ISender` entirely — see Code Review §1). An unbounded string here is not
  currently a crash risk (the AI runtime's mock router just echoes size in its output), but it is
  the one endpoint that ultimately drives LLM token spend once a real provider key is configured, so
  it's the highest-value place to add a length cap (e.g. 4,000 chars) before 1.0.
- No API-wide request body size limit is configured (`MaxRequestBodySize`/`RequestSizeLimit` not
  found anywhere) — Kestrel's default (30 MB) applies, which is generous for a JSON API with no
  file uploads.
- `resolve_sandboxed_path` (see §4) is itself a strong input-validation example on the Python side;
  it's the one place untrusted-shaped input (a path string an LLM-driven agent decided to write) is
  rigorously checked.

## 6. Dependency vulnerabilities

Checked all three manifests directly against their advisory databases:

**.NET** (`dotnet list package --vulnerable --include-transitive`):
- `Microsoft.OpenApi 2.0.0` (transitive, via the OpenAPI/Swagger tooling) — **High**,
  [GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc). No other package in
  `Domain`, `Application`, `Infrastructure`, or the test project is flagged.

**npm** (`npm audit`, 17 findings — 12 high, 4 moderate, 1 low), all transitive:
- `sharp` (bundled inside `next`'s image-optimization pipeline) — High, libvips CVEs.
- `postcss` (bundled inside `next`'s build pipeline) — High, XSS in stringify output +
  source-map path traversal/disclosure.
- `dompurify` (pulled in by `monaco-editor`) — Moderate, config-pollution/Trusted-Types bypass
  advisories.
- All three are build-time or editor-bundled dependencies, not directly reachable via
  user-controlled input in this app's actual usage (the app doesn't accept user-supplied CSS, and
  Monaco is used read-only for artifact previews) — real risk is low, but they should be tracked
  and cleared via `next`'s own upstream releases rather than a forced local patch (`npm audit fix
  --force` currently wants to downgrade `next` to `9.3.3`, which is not viable).

**Python** (`pip-audit`): the ambient environment this was run in was **not** an isolated venv built
from `ai-runtime/pyproject.toml` — it included unrelated packages (`torch`, `gitpython`) that are
not dependencies of this project, so those findings are noise, not this project's problem.
`ai-runtime`'s actual declared dependencies are minimal and pinned to recent major versions
(`fastapi>=0.115`, `uvicorn>=0.32`, `httpx>=0.27`, `redis>=5.1`, `pydantic>=2.9`,
`pydantic-settings>=2.6`) with no version-specific CVE identified against them at time of review.
**Recommendation**: add a CI step that runs `pip-audit` inside a clean venv built only from
`pyproject.toml`, so this check is trustworthy going forward instead of environment-dependent.

## 7. CORS

`Api/Program.cs` configures CORS via `WithOrigins(...)` (an explicit allowlist, driven by
`Cors:AllowedOrigins` config, defaulting to `http://localhost:3000`) plus `AllowCredentials()`.
This is the correct pattern — `AllowAnyOrigin()` combined with `AllowCredentials()` is rejected by
browsers and would be a real vulnerability if attempted; this codebase does not do that. No issue
found.

## 8. SignalR

- Hub methods (`JoinWorkflow`/`LeaveWorkflow`) take a raw `string workflowRunId` with no format
  validation — an arbitrary string becomes part of a Redis/in-memory group name
  (`workflow:{workflowRunId}`). Not currently exploitable (SignalR groups are just string keys, and
  nothing sensitive is keyed off the group name beyond routing), but worth validating as a GUID
  before use for defense-in-depth.
- As noted in §1, there is no authorization check on hub connection or group membership — anyone
  who can reach `/hubs/workflow` can join any run's group.
- No message-size or rate limit configured on the hub beyond SignalR's own defaults.

## 9. API exposure surface

- Every controller in `Api/Controllers` is reachable with no authentication (see §1) — this is the
  single overarching exposure concern; every other finding in this document is secondary to it.
- No `/health` or readiness endpoint was found exposing internal diagnostic detail beyond a simple
  liveness check — no information-disclosure concern there.
- The AI runtime (`ai-runtime`, port 8000) is directly reachable from the host in
  `docker-compose.yml`, in addition to being reachable from the API container over the internal
  Docker network. This is intentional for local development/debugging convenience, but a production
  deployment should not publish the AI runtime's port publicly at all — the frontend never talks to
  it directly (verified in the Code Review, frontend §7), so nothing outside the Docker network
  needs to reach it.

---

## Summary

| Severity | Finding | Area |
|---|---|---|
| **Critical (by design, must stay documented)** | No authentication/authorization anywhere | api, SignalR |
| High | `Microsoft.OpenApi` 2.0.0 known vulnerability | api (transitive) |
| Medium | `POST /api/intake` has zero input validation (no length cap) | api |
| Medium | 23 of 30 commands/queries have no FluentValidation validator | api |
| Medium | Docker containers run as root | api, ai-runtime |
| Low | SignalR hub accepts unvalidated group-name string | api |
| Low | npm transitive vulnerabilities (sharp/postcss/dompurify, build-time only) | frontend |
| — (strength) | Filesystem sandbox is genuinely well-defended, tested | ai-runtime |
| — (strength) | CORS correctly scoped, no wildcard+credentials | api |
| — (strength) | No secrets committed; `.gitignore` correctly scoped | all |

**Release 1.0 posture**: appropriate for a local/demo, single-operator deployment, which is this
project's stated scope. The no-auth boundary is the one item that must be impossible to miss by
anyone considering a wider deployment — it is called out in the README, `DEPLOYMENT.md`, and here.
