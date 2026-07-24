"""The standardized Reasoning Engine (ARCHITECTURE_EXTENSION.md §E6), implemented
exactly once. Every agent runs through this same 12-stage sequence:

    Observe -> Understand -> Think -> Plan -> RetrieveContext -> RetrieveMemory
    -> SelectTools -> Execute -> Reflect -> SelfCritique -> ConfidenceEvaluation
    -> PublishResult

Only Execute is agent-specific (each agent supplies its own domain logic via
an injected callable); every other stage has one generic implementation here.
This is what "specialized agents only implement their own domain logic" means
in practice — nothing about Observe/Plan/Memory/Tools/Critique/Publish is
duplicated per agent.
"""

from __future__ import annotations

import json
import time
import uuid
from collections.abc import Awaitable, Callable
from dataclasses import dataclass, field
from typing import Any

from app.clients.api_client import ApiClient
from app.clients.event_bus import RedisEventBus
from app.logging_config import get_logger, log_fields
from app.memory.memory_client import MemoryClient
from app.models.event_envelope import EventEnvelope, EventTypes
from app.routing.model_router import ModelRouter
from app.tools.registry import ToolRegistry

logger = get_logger(__name__)

RISK_KEYWORDS = ("uncertain", "unclear", "risk", "not sure", "todo", "incomplete", "assumption")


@dataclass
class AgentContext:
    task_node_id: uuid.UUID
    workflow_run_id: uuid.UUID
    workspace_id: uuid.UUID
    task_type: str
    name: str
    inputs: dict[str, Any]

    observations: Any = None
    understanding: Any = None
    thought: Any = None
    plan: Any = None
    context_data: str = ""
    memory_items: list[dict[str, Any]] = field(default_factory=list)
    selected_tools: list[str] = field(default_factory=list)
    execution_output: str = ""
    reflection: str = ""
    critique: str = ""
    confidence: float = 0.0
    risk_level: str = "medium"


@dataclass
class DomainResult:
    """What an agent's Execute stage produces — becomes the task's OutputsJson."""

    output_text: str
    artifact_name: str
    artifact_type: str
    extra_outputs: dict[str, Any] = field(default_factory=dict)


ExecuteFn = Callable[[AgentContext], Awaitable[DomainResult]]


class ReasoningPipeline:
    def __init__(
        self,
        agent_name: str,
        agent_capability: str,
        tools_available: list[str],
        api: ApiClient,
        memory: MemoryClient,
        tools: ToolRegistry,
        model_router: ModelRouter,
        event_bus: RedisEventBus,
    ) -> None:
        self._agent_name = agent_name
        self._agent_capability = agent_capability
        self._tools_available = tools_available
        self._api = api
        self._memory = memory
        self._tools = tools
        self._router = model_router
        self._event_bus = event_bus

    async def run(self, ctx: AgentContext, execute_fn: ExecuteFn) -> None:
        try:
            await self._stage(ctx, "Observe", self._observe)
            await self._stage(ctx, "Understand", self._understand)
            await self._stage(ctx, "Think", self._think)
            await self._stage(ctx, "Plan", self._plan)
            await self._stage(ctx, "RetrieveContext", self._retrieve_context)
            await self._stage(ctx, "RetrieveMemory", self._retrieve_memory)
            await self._stage(ctx, "SelectTools", self._select_tools)

            domain_result = await self._stage(
                ctx, "Execute", lambda c: self._execute(c, execute_fn)
            )

            await self._stage(ctx, "Reflect", self._reflect)
            await self._stage(ctx, "SelfCritique", self._self_critique)
            await self._stage(ctx, "ConfidenceEvaluation", self._evaluate_confidence)
            await self._stage(
                ctx, "PublishResult", lambda c: self._publish_result(c, domain_result, failed=False, reason=None)
            )

        except Exception as exc:  # noqa: BLE001 — deliberately broad: this is the task's failure boundary
            logger.exception(
                "task execution failed", extra={"fields": {"agent": self._agent_name, "task_node_id": str(ctx.task_node_id)}}
            )
            await self._stage(
                ctx, "PublishResult", lambda c: self._publish_result(c, None, failed=True, reason=str(exc))
            )

    # ---- generic stages ----

    async def _observe(self, ctx: AgentContext) -> Any:
        ctx.observations = ctx.inputs
        return ctx.observations

    async def _understand(self, ctx: AgentContext) -> Any:
        ctx.understanding = f"Task '{ctx.name}' ({ctx.task_type}) with {len(ctx.inputs)} input field(s)."
        return ctx.understanding

    async def _think(self, ctx: AgentContext) -> Any:
        ctx.thought = f"{self._agent_name} will address '{ctx.task_type}' using retrieved context and memory."
        return ctx.thought

    async def _plan(self, ctx: AgentContext) -> Any:
        ctx.plan = f"Plan: gather upstream context, recall relevant memory, execute domain logic, self-critique."
        return ctx.plan

    async def _retrieve_context(self, ctx: AgentContext) -> str:
        """Resolves upstream artifacts by NAME within this WorkflowRun, not by ID —
        a DAG node's InputsJson is fixed at node-creation time, before any
        predecessor (possibly a parallel one, e.g. Backend + Frontend feeding
        Code Reviewer) has actually produced its output artifact."""
        upstream_names = ctx.inputs.get("upstreamArtifactNames") or []
        parts: list[str] = []
        for artifact_name in upstream_names:
            try:
                artifact = await self._api.get_latest_artifact_by_name(ctx.workflow_run_id, artifact_name)
                if artifact is None:
                    logger.warning("upstream artifact not yet available", extra={"fields": {"name": artifact_name}})
                    continue
                parts.append(f"--- {artifact['name']} (by {artifact['ownerAgent']}) ---\n{artifact.get('content') or ''}")
            except Exception:
                logger.warning("could not retrieve upstream artifact", extra={"fields": {"name": artifact_name}})
        ctx.context_data = "\n\n".join(parts)
        return ctx.context_data

    async def _retrieve_memory(self, ctx: AgentContext) -> list[dict[str, Any]]:
        working = await self._memory.recall_working(ctx.task_node_id)
        project = await self._memory.recall_project()
        ctx.memory_items = working + project
        return ctx.memory_items

    async def _select_tools(self, ctx: AgentContext) -> list[str]:
        ctx.selected_tools = list(self._tools_available)
        return ctx.selected_tools

    async def _execute(self, ctx: AgentContext, execute_fn: ExecuteFn) -> DomainResult:
        result = await execute_fn(ctx)
        ctx.execution_output = result.output_text
        return result

    async def _reflect(self, ctx: AgentContext) -> str:
        ctx.reflection = f"Produced {len(ctx.execution_output)} characters of output for '{ctx.task_type}'."
        return ctx.reflection

    async def _self_critique(self, ctx: AgentContext) -> str:
        response = await self._router.complete(
            self._agent_capability,
            system_prompt="You are a rigorous self-critic. In 1-3 sentences, identify gaps, risks, or "
            "uncertainty in the following output. If it looks solid, say so plainly.",
            user_prompt=ctx.execution_output[:4000],
        )
        ctx.critique = response.text
        return ctx.critique

    async def _evaluate_confidence(self, ctx: AgentContext) -> tuple[float, str]:
        confidence = 0.85
        critique_lower = ctx.critique.lower()
        hits = sum(1 for kw in RISK_KEYWORDS if kw in critique_lower)
        confidence -= 0.1 * hits
        if not ctx.execution_output.strip():
            confidence -= 0.3
        confidence = max(0.4, min(0.97, confidence))

        risk_level = "low" if confidence >= 0.8 else "medium" if confidence >= 0.6 else "high"
        ctx.confidence = confidence
        ctx.risk_level = risk_level
        return confidence, risk_level

    async def _publish_result(
        self, ctx: AgentContext, domain_result: DomainResult | None, *, failed: bool, reason: str | None
    ) -> None:
        if failed:
            envelope = EventEnvelope(
                type=EventTypes.TASK_FAILED,
                workspace_id=ctx.workspace_id,
                workflow_run_id=ctx.workflow_run_id,
                task_id=ctx.task_node_id,
                produced_by=self._agent_name,
                payload_json=json.dumps({"TaskNodeId": str(ctx.task_node_id), "ReasonJson": json.dumps({"error": reason})}),
            )
            await self._event_bus.publish(envelope)
            return

        assert domain_result is not None
        outputs = {
            "text": domain_result.output_text,
            "artifactName": domain_result.artifact_name,
            "artifactType": domain_result.artifact_type,
            **domain_result.extra_outputs,
        }
        payload = {
            "TaskNodeId": str(ctx.task_node_id),
            "OutputsJson": json.dumps(outputs),
            "Confidence": ctx.confidence,
            "RiskLevel": ctx.risk_level,
            "ReasoningSummary": ctx.critique[:2000],
        }
        envelope = EventEnvelope(
            type=EventTypes.TASK_COMPLETED,
            workspace_id=ctx.workspace_id,
            workflow_run_id=ctx.workflow_run_id,
            task_id=ctx.task_node_id,
            produced_by=self._agent_name,
            payload_json=json.dumps(payload),
            confidence=ctx.confidence,
            risk_level=ctx.risk_level,
        )
        await self._event_bus.publish(envelope)

    # ---- stage timing/tracing/logging wrapper ----

    async def _stage(self, ctx: AgentContext, stage_name: str, fn: Callable[[AgentContext], Awaitable[Any]]) -> Any:
        started = time.perf_counter()
        error: str | None = None
        result: Any = None
        try:
            result = await fn(ctx)
            return result
        except Exception as exc:  # noqa: BLE001
            error = str(exc)
            raise
        finally:
            duration_ms = int((time.perf_counter() - started) * 1000)
            output_text = str(result) if result is not None else None

            log_fields(
                logger,
                40 if error else 20,
                f"reasoning stage: {stage_name}",
                agent=self._agent_name,
                stage=stage_name,
                duration_ms=duration_ms,
                confidence=ctx.confidence if stage_name == "ConfidenceEvaluation" else None,
                tool_usage=ctx.selected_tools if stage_name == "SelectTools" else None,
                memory_usage=len(ctx.memory_items) if stage_name == "RetrieveMemory" else None,
                tokens=len(output_text) if output_text else None,  # char-count placeholder — see model_router
                error=error,
            )

            try:
                await self._api.record_reasoning_trace(
                    task_node_id=ctx.task_node_id,
                    agent=self._agent_name,
                    stage=stage_name,
                    input_json=None,
                    output_json=(output_text or "")[:4000],
                    duration_ms=duration_ms,
                )
            except Exception:
                logger.warning("failed to persist reasoning trace", extra={"fields": {"stage": stage_name}})
