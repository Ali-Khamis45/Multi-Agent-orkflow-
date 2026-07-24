"""Supervisor Brain (ARCHITECTURE_EXTENSION.md §E1) — the executive AI. It
decides *why* and *what*: builds the execution DAG dynamically, chooses the
(Phase 1: fixed) execution strategy, and reacts to progress by expanding the
DAG and eventually producing a final execution summary.

It never dispatches a task itself — that mechanical work stays entirely with
the .NET Scheduler (§5.2), which the Supervisor only ever nudges via
`AddTaskNode`/`AddTaskDependency`/`RescheduleWorkflowRun`. "The Supervisor
decides. The Scheduler executes."
"""

from __future__ import annotations

import json
import uuid
from dataclasses import dataclass, field

from app.clients.api_client import ApiClient
from app.intent.intent_engine import IntentEngine
from app.logging_config import get_logger
from app.models.event_envelope import EventEnvelope, EventTypes

logger = get_logger(__name__)


@dataclass
class _RunState:
    workspace_id: uuid.UUID
    goal: str
    node_ids: dict[str, uuid.UUID] = field(default_factory=dict)  # friendly name -> TaskNodeId
    artifacts: dict[str, str] = field(default_factory=dict)  # friendly name -> artifactId
    expanded: bool = False
    finalized: bool = False


class SupervisorAgent:
    def __init__(self, api: ApiClient, intent_engine: IntentEngine) -> None:
        self._api = api
        self._intent_engine = intent_engine
        self._runs: dict[uuid.UUID, _RunState] = {}

    async def kickoff(self, workspace_id: uuid.UUID, raw_input: str) -> uuid.UUID:
        """User Request -> Intent Engine -> Business Analyst -> (Supervisor builds the rest)."""
        run_id = await self._api.create_workflow_run(workspace_id, goal=raw_input)
        state = _RunState(workspace_id=workspace_id, goal=raw_input)
        self._runs[run_id] = state

        structured_requirements_id = await self._intent_engine.run(workspace_id, run_id, raw_input)
        state.artifacts["StructuredRequirements"] = str(structured_requirements_id)

        ba_id = await self._api.add_task_node(
            run_id,
            name="BusinessAnalysis",
            task_type="DiscoverRequirements",
            inputs_json=json.dumps({"goal": raw_input, "upstreamArtifactNames": ["StructuredRequirements"]}),
        )
        state.node_ids["BusinessAnalysis"] = ba_id

        await self._api.record_supervisor_decision(
            run_id,
            decision_type="StrategySelection",
            input_snapshot_json=json.dumps({"goal": raw_input}),
            rationale=(
                "Phase 1 fixed pipeline: Business Analyst first; once it completes, expand the DAG "
                "with Project Manager -> System Architect -> {Backend, Frontend} (parallel) -> "
                "Code Reviewer -> QA Engineer."
            ),
            confidence=1.0,
        )

        await self._api.start_workflow_run(run_id)
        logger.info("workflow kicked off", extra={"fields": {"runId": str(run_id), "baNodeId": str(ba_id)}})
        return run_id

    async def on_event(self, envelope: EventEnvelope) -> None:
        if envelope.workflow_run_id is None or envelope.workflow_run_id not in self._runs:
            return

        state = self._runs[envelope.workflow_run_id]

        if envelope.type == EventTypes.TASK_COMPLETED:
            await self._record_artifact(state, envelope)

            if not state.expanded and envelope.task_id == state.node_ids.get("BusinessAnalysis"):
                await self._expand_dag(envelope.workflow_run_id, state)

            if state.expanded and not state.finalized and envelope.task_id == state.node_ids.get("QAValidation"):
                await self._finalize(envelope.workflow_run_id, state)

        elif envelope.type == EventTypes.TASK_FAILED:
            await self._api.record_supervisor_decision(
                envelope.workflow_run_id,
                decision_type="Retry",
                input_snapshot_json=envelope.payload_json,
                rationale="TaskFailed observed; the Scheduler's own retry policy (§10) is handling "
                "reassignment — recorded here for audit/observability.",
                confidence=0.5,
                target_node_ids_json=json.dumps([str(envelope.task_id)] if envelope.task_id else []),
            )

    async def _record_artifact(self, state: _RunState, envelope: EventEnvelope) -> None:
        payload = json.loads(envelope.payload_json)
        try:
            outputs = json.loads(payload.get("OutputsJson") or "{}")
        except json.JSONDecodeError:
            return
        artifact_name = outputs.get("artifactName")
        artifact_id = outputs.get("artifactId")
        if artifact_name and artifact_id:
            state.artifacts[artifact_name] = artifact_id

    async def _expand_dag(self, run_id: uuid.UUID, state: _RunState) -> None:
        goal = state.goal

        async def add(name: str, task_type: str, upstream: list[str]) -> uuid.UUID:
            node_id = await self._api.add_task_node(
                run_id, name=name, task_type=task_type,
                inputs_json=json.dumps({"goal": goal, "upstreamArtifactNames": upstream}),
            )
            state.node_ids[name] = node_id
            return node_id

        pm_id = await add("ProjectPlanning", "PlanProject", ["UserStories"])
        await self._api.add_task_dependency(run_id, state.node_ids["BusinessAnalysis"], pm_id)

        arch_id = await add("ArchitectureDesign", "DesignArchitecture", ["TaskPlan"])
        await self._api.add_task_dependency(run_id, pm_id, arch_id)

        backend_id = await add("BackendImplementation", "ImplementBackendFeature", ["ArchitectureDoc"])
        await self._api.add_task_dependency(run_id, arch_id, backend_id)

        frontend_id = await add("FrontendImplementation", "ImplementFrontendFeature", ["ArchitectureDoc"])
        await self._api.add_task_dependency(run_id, arch_id, frontend_id)

        review_id = await add("CodeReview", "ReviewCode", ["BackendCode", "FrontendCode"])
        await self._api.add_task_dependency(run_id, backend_id, review_id)
        await self._api.add_task_dependency(run_id, frontend_id, review_id)

        qa_id = await add("QAValidation", "RunQA", ["CodeReviewReport"])
        await self._api.add_task_dependency(run_id, review_id, qa_id)

        state.expanded = True

        await self._api.record_supervisor_decision(
            run_id,
            decision_type="StrategySelection",
            input_snapshot_json=json.dumps({"expandedAfter": "BusinessAnalysis"}),
            rationale="Business Analyst completed; expanded DAG with PM -> Architect -> "
            "{Backend, Frontend} (parallel) -> Code Review -> QA.",
            confidence=0.95,
        )

        # Nodes were just added to an already-Running WorkflowRun — the Scheduler
        # only recomputes the ready set on task completion/dispatch, so it must be
        # told explicitly that the DAG changed.
        await self._api.reschedule_workflow_run(run_id)
        logger.info("DAG expanded", extra={"fields": {"runId": str(run_id), "newNodes": list(state.node_ids.keys())}})

    async def _finalize(self, run_id: uuid.UUID, state: _RunState) -> None:
        state.finalized = True
        lines = ["# Execution Summary\n", f"**Goal:** {state.goal}\n", "## Produced Artifacts\n"]
        for name, node_id in state.node_ids.items():
            artifact_id = state.artifacts.get(_ARTIFACT_NAME_BY_NODE.get(name, ""), "n/a")
            lines.append(f"- **{name}** (task `{node_id}`) -> artifact `{artifact_id}`")

        content = "\n".join(lines)
        await self._api.create_artifact(
            workspace_id=state.workspace_id,
            name="ExecutionSummary",
            artifact_type="Markdown",
            owner_agent="supervisor",
            content=content,
            workflow_run_id=run_id,
        )
        logger.info("workflow finalized", extra={"fields": {"runId": str(run_id)}})


_ARTIFACT_NAME_BY_NODE = {
    "BusinessAnalysis": "UserStories",
    "ProjectPlanning": "TaskPlan",
    "ArchitectureDesign": "ArchitectureDoc",
    "BackendImplementation": "BackendCode",
    "FrontendImplementation": "FrontendCode",
    "CodeReview": "CodeReviewReport",
    "QAValidation": "QAReport",
}
