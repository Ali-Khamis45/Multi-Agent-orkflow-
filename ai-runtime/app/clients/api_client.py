"""The only way this runtime touches persistence (ARCHITECTURE.md §2, §23):
every workflow/registry/artifact/memory/reasoning fact goes through the
.NET Orchestration Service's REST API. No agent, tool, or pipeline stage in
this codebase is allowed to open a database connection directly.
"""

from __future__ import annotations

import json
import uuid
from typing import Any

import httpx


class ApiClient:
    def __init__(self, base_url: str, internal_service_key: str | None = None) -> None:
        self._http = httpx.AsyncClient(base_url=base_url, timeout=30.0)
        self._internal_service_key = internal_service_key

    async def aclose(self) -> None:
        await self._http.aclose()

    # ---- Workspaces (§23) ----
    async def create_workspace(self, name: str) -> uuid.UUID:
        """Service-to-service call (Phase 2) — this process has no user session,
        so it authenticates with the shared internal service key rather than a
        JWT. Used only for this runtime's own legacy default-workspace bootstrap;
        every real user-initiated workflow already has a workspace_id from the
        authenticated frontend by the time it reaches /intake."""
        headers = {"X-Internal-Service-Key": self._internal_service_key} if self._internal_service_key else {}
        r = await self._http.post("/api/workspaces", json={"name": name}, headers=headers)
        r.raise_for_status()
        return uuid.UUID(r.json())

    # ---- Dynamic Agent Registry (§4.2) ----
    async def register_agent(self, manifest: dict[str, Any]) -> uuid.UUID:
        r = await self._http.post("/api/registry/agents", json=manifest)
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def heartbeat(self, name: str, company_type: str = "SoftwareCompany") -> bool:
        r = await self._http.put(f"/api/registry/agents/{name}/heartbeat", params={"companyType": company_type})
        return r.status_code == 204

    async def get_agents(self) -> list[dict[str, Any]]:
        r = await self._http.get("/api/registry/agents")
        r.raise_for_status()
        return r.json()

    # ---- Workflow Engine (§5, §23) ----
    async def create_workflow_run(
        self, workspace_id: uuid.UUID, goal: str, correlation_id: uuid.UUID | None = None
    ) -> uuid.UUID:
        r = await self._http.post(
            "/api/workflows/runs",
            json={
                "workspaceId": str(workspace_id),
                "goal": goal,
                "correlationId": str(correlation_id) if correlation_id else None,
            },
        )
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def add_task_node(
        self,
        run_id: uuid.UUID,
        name: str,
        task_type: str,
        inputs_json: str,
        requires_approval: bool = False,
    ) -> uuid.UUID:
        r = await self._http.post(
            f"/api/workflows/runs/{run_id}/nodes",
            json={
                "name": name,
                "taskType": task_type,
                "inputsJson": inputs_json,
                "requiresApproval": requires_approval,
            },
        )
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def add_task_dependency(
        self, run_id: uuid.UUID, predecessor_node_id: uuid.UUID, successor_node_id: uuid.UUID
    ) -> uuid.UUID:
        r = await self._http.post(
            f"/api/workflows/runs/{run_id}/edges",
            json={
                "predecessorNodeId": str(predecessor_node_id),
                "successorNodeId": str(successor_node_id),
            },
        )
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def start_workflow_run(self, run_id: uuid.UUID) -> None:
        r = await self._http.post(f"/api/workflows/runs/{run_id}/start")
        r.raise_for_status()

    async def reschedule_workflow_run(self, run_id: uuid.UUID) -> None:
        r = await self._http.post(f"/api/workflows/runs/{run_id}/reschedule")
        r.raise_for_status()

    async def get_workflow_run(self, run_id: uuid.UUID) -> dict[str, Any]:
        r = await self._http.get(f"/api/workflows/runs/{run_id}")
        r.raise_for_status()
        return r.json()

    # ---- Intent Engine (§E2) ----
    async def start_intent_session(
        self, workspace_id: uuid.UUID, raw_input: str, correlation_id: uuid.UUID | None = None
    ) -> uuid.UUID:
        r = await self._http.post(
            "/api/intent/sessions",
            json={
                "workspaceId": str(workspace_id),
                "rawInput": raw_input,
                "correlationId": str(correlation_id) if correlation_id else None,
            },
        )
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def record_intent_analysis(
        self,
        session_id: uuid.UUID,
        extracted_goals_json: str,
        project_classification: str,
        complexity_score: float,
        risk_flags_json: str,
        ambiguities_json: str,
        has_ambiguity: bool,
    ) -> None:
        r = await self._http.post(
            f"/api/intent/sessions/{session_id}/analysis",
            json={
                "extractedGoalsJson": extracted_goals_json,
                "projectClassification": project_classification,
                "complexityScore": complexity_score,
                "riskFlagsJson": risk_flags_json,
                "ambiguitiesJson": ambiguities_json,
                "hasAmbiguity": has_ambiguity,
            },
        )
        r.raise_for_status()

    async def mark_intent_structured(self, session_id: uuid.UUID, artifact_id: uuid.UUID) -> None:
        r = await self._http.post(
            f"/api/intent/sessions/{session_id}/structure",
            json={"structuredRequirementsArtifactId": str(artifact_id)},
        )
        r.raise_for_status()

    async def get_intent_session(self, session_id: uuid.UUID) -> dict[str, Any]:
        r = await self._http.get(f"/api/intent/sessions/{session_id}")
        r.raise_for_status()
        return r.json()

    # ---- Artifact Manager (§14.1) ----
    async def create_artifact(
        self,
        workspace_id: uuid.UUID,
        name: str,
        artifact_type: str,
        owner_agent: str,
        content: str | None,
        workflow_run_id: uuid.UUID | None = None,
        task_node_id: uuid.UUID | None = None,
        previous_version_id: uuid.UUID | None = None,
        correlation_id: uuid.UUID | None = None,
        idempotency_key: str | None = None,
    ) -> uuid.UUID:
        r = await self._http.post(
            "/api/artifacts",
            json={
                "workspaceId": str(workspace_id),
                "name": name,
                "type": artifact_type,
                "ownerAgent": owner_agent,
                "content": content,
                "workflowRunId": str(workflow_run_id) if workflow_run_id else None,
                "taskNodeId": str(task_node_id) if task_node_id else None,
                "previousVersionId": str(previous_version_id) if previous_version_id else None,
                "correlationId": str(correlation_id) if correlation_id else None,
                "idempotencyKey": idempotency_key,
            },
        )
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def get_artifact(self, artifact_id: uuid.UUID) -> dict[str, Any]:
        r = await self._http.get(f"/api/artifacts/{artifact_id}")
        r.raise_for_status()
        return r.json()

    async def get_latest_artifact_by_name(self, workflow_run_id: uuid.UUID, name: str) -> dict[str, Any] | None:
        r = await self._http.get(
            "/api/artifacts/by-name", params={"workflowRunId": str(workflow_run_id), "name": name}
        )
        if r.status_code == 404:
            return None
        r.raise_for_status()
        return r.json()

    # ---- Multi-Layer Memory (§E5) ----
    async def write_memory(
        self,
        workspace_id: uuid.UUID,
        layer: str,
        scope_ref: uuid.UUID,
        kind: str,
        content: str,
        source_artifact_id: uuid.UUID | None = None,
        correlation_id: uuid.UUID | None = None,
    ) -> uuid.UUID:
        r = await self._http.post(
            "/api/memory",
            json={
                "workspaceId": str(workspace_id),
                "layer": layer,
                "scopeRef": str(scope_ref),
                "kind": kind,
                "content": content,
                "sourceArtifactId": str(source_artifact_id) if source_artifact_id else None,
                "ttlAt": None,
                "correlationId": str(correlation_id) if correlation_id else None,
            },
        )
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def query_memory(
        self, workspace_id: uuid.UUID, layer: str, scope_ref: uuid.UUID, limit: int = 20
    ) -> list[dict[str, Any]]:
        r = await self._http.get(
            "/api/memory",
            params={"workspaceId": str(workspace_id), "layer": layer, "scopeRef": str(scope_ref), "limit": limit},
        )
        r.raise_for_status()
        return r.json()

    # ---- Company Profile / Company Memory (Phase 3 "AI Company Operating System") ----
    async def get_company_profile(self, workspace_id: uuid.UUID) -> dict[str, Any]:
        """Full CompanyProfileDto — `isOnboarded` plus the raw (still-JSON-string)
        `profileJson`. Callers that only want the parsed profile shape (e.g. the
        reasoning pipeline's Company Memory context) do `json.loads(result["profileJson"])`
        themselves rather than this client silently deciding that for every caller."""
        headers = {"X-Internal-Service-Key": self._internal_service_key} if self._internal_service_key else {}
        r = await self._http.get(
            "/api/company-profile", params={"workspaceId": str(workspace_id)}, headers=headers
        )
        r.raise_for_status()
        return r.json()

    async def patch_company_profile_section(
        self, workspace_id: uuid.UUID, section: str, patch: dict[str, Any]
    ) -> None:
        headers = {"X-Internal-Service-Key": self._internal_service_key} if self._internal_service_key else {}
        r = await self._http.patch(
            "/api/company-profile/section",
            json={"workspaceId": str(workspace_id), "section": section, "patch": patch},
            headers=headers,
        )
        r.raise_for_status()

    # ---- Reasoning Engine traces / unified telemetry (§E6, Phase 1.5 §1) ----
    async def record_reasoning_trace(
        self,
        task_node_id: uuid.UUID,
        agent: str,
        stage: str,
        started_at: str,
        duration_ms: int,
        input_json: str | None = None,
        output_json: str | None = None,
        tokens: int | None = None,
        confidence: float | None = None,
        model_used: str | None = None,
        retry_count: int = 0,
        memory_reads: int = 0,
        memory_writes: int = 0,
        tool_calls: int = 0,
        cost_estimate: float | None = None,
        error_message: str | None = None,
    ) -> None:
        r = await self._http.post(
            "/api/reasoning/traces",
            json={
                "taskNodeId": str(task_node_id),
                "agent": agent,
                "stage": stage,
                "startedAt": started_at,
                "durationMs": duration_ms,
                "inputJson": input_json,
                "outputJson": output_json,
                "tokens": tokens,
                "confidence": confidence,
                "modelUsed": model_used,
                "retryCount": retry_count,
                "memoryReads": memory_reads,
                "memoryWrites": memory_writes,
                "toolCalls": tool_calls,
                "costEstimate": cost_estimate,
                "errorMessage": error_message,
            },
        )
        r.raise_for_status()

    # ---- Supervisor Brain audit trail (§E1) ----
    async def record_supervisor_decision(
        self,
        workflow_run_id: uuid.UUID,
        decision_type: str,
        input_snapshot_json: str,
        rationale: str,
        confidence: float,
        target_node_ids_json: str | None = None,
    ) -> uuid.UUID:
        r = await self._http.post(
            "/api/supervisor/decisions",
            json={
                "workflowRunId": str(workflow_run_id),
                "decisionType": decision_type,
                "inputSnapshotJson": input_snapshot_json,
                "rationale": rationale,
                "confidence": confidence,
                "targetNodeIdsJson": target_node_ids_json,
            },
        )
        r.raise_for_status()
        return uuid.UUID(r.json())
