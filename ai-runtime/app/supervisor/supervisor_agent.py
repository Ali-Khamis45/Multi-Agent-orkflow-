"""Supervisor Brain (ARCHITECTURE_EXTENSION.md §E1) — the executive AI. It
decides *why* and *what*: builds the execution DAG dynamically, chooses the
(Phase 1: fixed, per-CompanyType) execution strategy, and reacts to progress by
expanding the DAG and eventually producing a final execution summary.

It never dispatches a task itself — that mechanical work stays entirely with
the .NET Scheduler (§5.2), which the Supervisor only ever nudges via
`AddTaskNode`/`AddTaskDependency`/`RescheduleWorkflowRun`. "The Supervisor
decides. The Scheduler executes."

Phase 2 ("AI Enterprise OS"): a workflow's CompanyType comes from the
submitting user's account (fixed at registration — see docs/architecture/
OVERVIEW.md), not from re-classifying the request, so there is no separate
"which company is this for" routing step here. What *is* CompanyType-specific
is which fixed pipeline gets built — the SoftwareCompany pipeline below is
unchanged from Phase 1; the Founder pipeline is new.
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
    correlation_id: uuid.UUID
    goal: str
    company_type: str
    node_ids: dict[str, uuid.UUID] = field(default_factory=dict)  # friendly name -> TaskNodeId
    artifacts: dict[str, str] = field(default_factory=dict)  # friendly name -> artifactId
    expanded: bool = False
    finalized: bool = False


class SupervisorAgent:
    def __init__(self, api: ApiClient, intent_engine: IntentEngine) -> None:
        self._api = api
        self._intent_engine = intent_engine
        self._runs: dict[uuid.UUID, _RunState] = {}

    async def kickoff(self, workspace_id: uuid.UUID, raw_input: str, company_type: str = "SoftwareCompany") -> uuid.UUID:
        """User Request -> Intent Engine -> first task -> (Supervisor builds the rest).

        Mints the CorrelationId (Phase 1.5 §2) here — the earliest point in the
        whole execution — and threads it through every subsequent call this
        workflow run makes, across both runtimes.
        """
        correlation_id = uuid.uuid4()
        run_id = await self._api.create_workflow_run(workspace_id, goal=raw_input, correlation_id=correlation_id)
        state = _RunState(workspace_id=workspace_id, correlation_id=correlation_id, goal=raw_input, company_type=company_type)
        self._runs[run_id] = state

        structured_requirements_id = await self._intent_engine.run(
            workspace_id, run_id, raw_input, correlation_id=correlation_id
        )
        state.artifacts["StructuredRequirements"] = str(structured_requirements_id)

        if company_type == "Founder":
            first_name, first_task_type, rationale = (
                "VentureFraming", "FrameVenture",
                "Founder fixed pipeline: CEO frames the venture first; once it completes, expand the "
                "DAG with Business Model Analysis -> {Market, Customer} Research (parallel) -> Brand "
                "Strategy -> {Financial, Marketing, Operations, Sales} Planning (parallel) -> Growth "
                "Planning -> Legal Review.",
            )
        else:
            first_name, first_task_type, rationale = (
                "BusinessAnalysis", "DiscoverRequirements",
                "Phase 1 fixed pipeline: Business Analyst first; once it completes, expand the DAG "
                "with Project Manager -> System Architect -> {Backend, Frontend} (parallel) -> "
                "Code Reviewer -> QA Engineer.",
            )

        first_id = await self._api.add_task_node(
            run_id,
            name=first_name,
            task_type=first_task_type,
            inputs_json=json.dumps({"goal": raw_input, "upstreamArtifactNames": ["StructuredRequirements"]}),
        )
        state.node_ids[first_name] = first_id

        await self._api.record_supervisor_decision(
            run_id,
            decision_type="StrategySelection",
            input_snapshot_json=json.dumps({"goal": raw_input, "companyType": company_type}),
            rationale=rationale,
            confidence=1.0,
        )

        await self._api.start_workflow_run(run_id)
        logger.info(
            "workflow kicked off",
            extra={"fields": {"runId": str(run_id), "companyType": company_type, "firstNodeId": str(first_id)}},
        )
        return run_id

    async def on_event(self, envelope: EventEnvelope) -> None:
        if envelope.workflow_run_id is None or envelope.workflow_run_id not in self._runs:
            return

        state = self._runs[envelope.workflow_run_id]

        if envelope.type == EventTypes.TASK_COMPLETED:
            await self._record_artifact(state, envelope)

            first_name = "VentureFraming" if state.company_type == "Founder" else "BusinessAnalysis"
            last_name = "LegalReview" if state.company_type == "Founder" else "QAValidation"

            if not state.expanded and envelope.task_id == state.node_ids.get(first_name):
                if state.company_type == "Founder":
                    await self._expand_founder_dag(envelope.workflow_run_id, state)
                else:
                    await self._expand_software_dag(envelope.workflow_run_id, state)

            if state.expanded and not state.finalized and envelope.task_id == state.node_ids.get(last_name):
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

    async def _expand_software_dag(self, run_id: uuid.UUID, state: _RunState) -> None:
        goal = state.goal
        add = self._node_adder(run_id, state)

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
        await self._api.reschedule_workflow_run(run_id)
        logger.info("DAG expanded", extra={"fields": {"runId": str(run_id), "newNodes": list(state.node_ids.keys())}})

    async def _expand_founder_dag(self, run_id: uuid.UUID, state: _RunState) -> None:
        add = self._node_adder(run_id, state)

        bma_id = await add("BusinessModelAnalysis", "AnalyzeBusinessModel", ["ExecutiveSummary"])
        await self._api.add_task_dependency(run_id, state.node_ids["VentureFraming"], bma_id)

        market_id = await add("MarketResearch", "ResearchMarket", ["BusinessModelCanvas"])
        await self._api.add_task_dependency(run_id, bma_id, market_id)

        customer_id = await add("CustomerResearch", "ResearchCustomers", ["BusinessModelCanvas"])
        await self._api.add_task_dependency(run_id, bma_id, customer_id)

        brand_id = await add("BrandStrategy", "DefineBrandIdentity", ["MarketResearchReport", "CustomerPersonas"])
        await self._api.add_task_dependency(run_id, market_id, brand_id)
        await self._api.add_task_dependency(run_id, customer_id, brand_id)

        financial_id = await add("FinancialPlanning", "ProjectFinancials", ["BrandIdentity"])
        await self._api.add_task_dependency(run_id, brand_id, financial_id)

        marketing_id = await add("MarketingPlanning", "CreateMarketingPlan", ["BrandIdentity"])
        await self._api.add_task_dependency(run_id, brand_id, marketing_id)

        operations_id = await add("OperationsPlanning", "PlanOperations", ["BrandIdentity"])
        await self._api.add_task_dependency(run_id, brand_id, operations_id)

        sales_id = await add("SalesPlanning", "DefineSalesStrategy", ["BrandIdentity"])
        await self._api.add_task_dependency(run_id, brand_id, sales_id)

        growth_id = await add(
            "GrowthPlanning", "PlanGrowth",
            ["FinancialProjection", "MarketingPlan", "OperationsPlan", "SalesStrategy"],
        )
        for predecessor_id in (financial_id, marketing_id, operations_id, sales_id):
            await self._api.add_task_dependency(run_id, predecessor_id, growth_id)

        legal_id = await add("LegalReview", "ReviewLegalRisk", ["GrowthRoadmap"])
        await self._api.add_task_dependency(run_id, growth_id, legal_id)

        state.expanded = True
        await self._api.record_supervisor_decision(
            run_id,
            decision_type="StrategySelection",
            input_snapshot_json=json.dumps({"expandedAfter": "VentureFraming"}),
            rationale="CEO framing completed; expanded DAG with Business Model Analysis -> "
            "{Market, Customer} Research (parallel) -> Brand Strategy -> {Financial, Marketing, "
            "Operations, Sales} Planning (parallel) -> Growth Planning -> Legal Review.",
            confidence=0.95,
        )
        await self._api.reschedule_workflow_run(run_id)
        logger.info("DAG expanded", extra={"fields": {"runId": str(run_id), "newNodes": list(state.node_ids.keys())}})

    def _node_adder(self, run_id: uuid.UUID, state: _RunState):
        async def add(name: str, task_type: str, upstream: list[str]) -> uuid.UUID:
            node_id = await self._api.add_task_node(
                run_id, name=name, task_type=task_type,
                inputs_json=json.dumps({"goal": state.goal, "upstreamArtifactNames": upstream}),
            )
            state.node_ids[name] = node_id
            return node_id

        return add

    async def _finalize(self, run_id: uuid.UUID, state: _RunState) -> None:
        state.finalized = True
        artifact_name_by_node = _FOUNDER_ARTIFACT_NAME_BY_NODE if state.company_type == "Founder" else _SOFTWARE_ARTIFACT_NAME_BY_NODE

        lines = ["# Execution Summary\n", f"**Goal:** {state.goal}\n", "## Produced Artifacts\n"]
        for name, node_id in state.node_ids.items():
            artifact_id = state.artifacts.get(artifact_name_by_node.get(name, ""), "n/a")
            lines.append(f"- **{name}** (task `{node_id}`) -> artifact `{artifact_id}`")

        content = "\n".join(lines)
        await self._api.create_artifact(
            workspace_id=state.workspace_id,
            name="ExecutionSummary",
            artifact_type="Markdown",
            owner_agent="supervisor",
            content=content,
            workflow_run_id=run_id,
            correlation_id=state.correlation_id,
            idempotency_key=f"{run_id}:ExecutionSummary",
        )
        logger.info("workflow finalized", extra={"fields": {"runId": str(run_id)}})


_SOFTWARE_ARTIFACT_NAME_BY_NODE = {
    "BusinessAnalysis": "UserStories",
    "ProjectPlanning": "TaskPlan",
    "ArchitectureDesign": "ArchitectureDoc",
    "BackendImplementation": "BackendCode",
    "FrontendImplementation": "FrontendCode",
    "CodeReview": "CodeReviewReport",
    "QAValidation": "QAReport",
}

_FOUNDER_ARTIFACT_NAME_BY_NODE = {
    "VentureFraming": "ExecutiveSummary",
    "BusinessModelAnalysis": "BusinessModelCanvas",
    "MarketResearch": "MarketResearchReport",
    "CustomerResearch": "CustomerPersonas",
    "BrandStrategy": "BrandIdentity",
    "FinancialPlanning": "FinancialProjection",
    "MarketingPlanning": "MarketingPlan",
    "OperationsPlanning": "OperationsPlan",
    "SalesPlanning": "SalesStrategy",
    "GrowthPlanning": "GrowthRoadmap",
    "LegalReview": "LaunchStrategy",
}
