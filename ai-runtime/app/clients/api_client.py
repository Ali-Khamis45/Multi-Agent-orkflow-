"""The only way this runtime touches persistence (ARCHITECTURE.md §2, §23):
every workflow/registry/artifact/memory/reasoning fact goes through the
.NET Orchestration Service's REST API. No agent, tool, or pipeline stage in
this codebase is allowed to open a database connection directly.
"""

from __future__ import annotations

import uuid
from typing import Any

import httpx


class ApiClient:
    def __init__(self, base_url: str) -> None:
        self._http = httpx.AsyncClient(base_url=base_url, timeout=30.0)

    async def aclose(self) -> None:
        await self._http.aclose()

    # ---- Workspaces (§23) ----
    async def create_workspace(self, name: str) -> uuid.UUID:
        r = await self._http.post("/api/workspaces", json={"name": name})
        r.raise_for_status()
        return uuid.UUID(r.json())

    # ---- Dynamic Agent Registry (§4.2) ----
    async def register_agent(self, manifest: dict[str, Any]) -> uuid.UUID:
        r = await self._http.post("/api/registry/agents", json=manifest)
        r.raise_for_status()
        return uuid.UUID(r.json())

    async def heartbeat(self, name: str) -> bool:
        r = await self._http.put(f"/api/registry/agents/{name}/heartbeat")
        return r.status_code == 204

    async def get_agents(self) -> list[dict[str, Any]]:
        r = await self._http.get("/api/registry/agents")
        r.raise_for_status()
        return r.json()

    # ---- Workflow Engine (§5, §23) ----
    async def create_workflow_run(self, workspace_id: uuid.UUID, goal: str) -> uuid.UUID:
        r = await self._http.post(
            "/api/workflows/runs", json={"workspaceId": str(workspace_id), "goal": goal}
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
    async def start_intent_session(self, workspace_id: uuid.UUID, raw_input: str) -> uuid.UUID:
        r = await self._http.post(
            "/api/intent/sessions", json={"workspaceId": str(workspace_id), "rawInput": raw_input}
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

    # ---- Reasoning Engine traces (§E6) ----
    async def record_reasoning_trace(
        self,
        task_node_id: uuid.UUID,
        agent: str,
        stage: str,
        input_json: str | None,
        output_json: str | None,
        duration_ms: int,
    ) -> None:
        r = await self._http.post(
            "/api/reasoning/traces",
            json={
                "taskNodeId": str(task_node_id),
                "agent": agent,
                "stage": stage,
                "inputJson": input_json,
                "outputJson": output_json,
                "durationMs": duration_ms,
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
