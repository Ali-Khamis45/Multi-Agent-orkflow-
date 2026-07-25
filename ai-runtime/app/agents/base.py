"""AgentBase (per this milestone's instructions): every agent inherits the
reasoning pipeline, memory access, tool access, event publishing, structured
logging, metrics, confidence calculation (via the pipeline), and a retry
helper from here. A concrete agent implements exactly one thing:
`execute_domain_logic` — its own Think/Plan/Execute-specific behavior.
"""

from __future__ import annotations

import asyncio
import json
import uuid
from abc import ABC, abstractmethod
from collections.abc import Awaitable, Callable
from typing import Any, TypeVar

from app.clients.api_client import ApiClient
from app.clients.event_bus import RedisEventBus
from app.logging_config import get_logger, log_fields
from app.memory.memory_client import MemoryClient
from app.models.event_envelope import EventEnvelope
from app.reasoning.pipeline import AgentContext, DomainResult, ReasoningPipeline
from app.routing.model_router import ModelRouter
from app.tools.registry import ToolRegistry

T = TypeVar("T")


class AgentBase(ABC):
    name: str
    version: str = "0.1.0"
    description: str
    # Which specialized AI company this agent belongs to (Phase 2, "AI Enterprise
    # OS"). Every agent that existed before Phase 2 is implicitly a SoftwareCompany
    # agent; only Founder-workspace agents need to set this explicitly.
    company_type: str = "SoftwareCompany"
    skills: list[str] = []
    supported_tasks: list[str] = []
    priority: int = 50
    required_context: list[str] = []
    produced_artifacts: list[str] = []
    dependencies: list[str] = []
    tools_available: list[str] = []
    permissions: list[str] = []

    def __init__(
        self,
        api: ApiClient,
        memory: MemoryClient,
        tools: ToolRegistry,
        event_bus: RedisEventBus,
        endpoint: str,
        model_router: ModelRouter,
    ) -> None:
        self.api = api
        self.memory = memory
        self.tools = tools
        self.event_bus = event_bus
        self.endpoint = endpoint
        self.model_router = model_router
        self.logger = get_logger(f"agent.{self.name}")
        self.metrics = {"invocations": 0, "successes": 0, "failures": 0}

        self.pipeline = ReasoningPipeline(
            agent_name=self.name,
            agent_capability=self.name,
            tools_available=self.tools_available,
            api=api,
            memory=memory,
            tools=tools,
            model_router=model_router,
            event_bus=event_bus,
        )

    def manifest(self) -> dict[str, Any]:
        """The self-registration manifest (ARCHITECTURE.md §4.1)."""
        return {
            "name": self.name,
            "companyType": self.company_type,
            "version": self.version,
            "description": self.description,
            "skills": self.skills,
            "supportedTasks": self.supported_tasks,
            "priority": self.priority,
            "requiredContext": self.required_context,
            "producedArtifacts": self.produced_artifacts,
            "dependencies": self.dependencies,
            "tools": self.tools_available,
            "permissions": self.permissions,
            "endpoint": self.endpoint,
            "healthCheck": None,
        }

    async def register(self, workspace_id: uuid.UUID) -> None:
        await self.api.register_agent({**self.manifest(), "workspaceId": str(workspace_id)})
        log_fields(self.logger, 20, "agent registered", agent=self.name, supported_tasks=self.supported_tasks)

    async def heartbeat_loop(self, interval_seconds: float, stop_event: asyncio.Event) -> None:
        while not stop_event.is_set():
            try:
                await self.api.heartbeat(self.name, self.company_type)
            except Exception:
                self.logger.warning("heartbeat failed", extra={"fields": {"agent": self.name}})
            try:
                await asyncio.wait_for(stop_event.wait(), timeout=interval_seconds)
            except TimeoutError:
                pass

    async def handle_task_dispatched(self, envelope: EventEnvelope) -> None:
        """WorkspaceId/WorkflowRunId/TaskId/CorrelationId live on the envelope
        itself (§6.2, Phase 1.5 §2); only task-shape fields (TaskType, Name,
        InputsJson, AttemptCount) are in the payload."""
        self.metrics["invocations"] += 1
        payload = json.loads(envelope.payload_json)
        assert envelope.workflow_run_id is not None
        assert envelope.task_id is not None

        ctx = AgentContext(
            task_node_id=envelope.task_id,
            workflow_run_id=envelope.workflow_run_id,
            workspace_id=envelope.workspace_id,
            correlation_id=envelope.correlation_id or uuid.uuid4(),
            task_type=payload["TaskType"],
            name=payload["Name"],
            inputs=json.loads(payload.get("InputsJson") or "{}"),
            retry_count=payload.get("AttemptCount", 1) - 1,
        )
        try:
            await self.pipeline.run(ctx, self.execute_domain_logic)
            self.metrics["successes"] += 1
        except Exception:
            self.metrics["failures"] += 1
            raise

    @abstractmethod
    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        """The only method a concrete agent must implement — its domain-specific
        Think/Plan/Execute logic. Everything else is handled by ReasoningPipeline."""

    # ---- shared helpers available to every concrete agent's execute_domain_logic ----

    async def generate(self, ctx: AgentContext, prompt_template: str, **variables: Any) -> str:
        """Loads a prompt template (Prompt Loader tool, §8) and completes it via
        the Multi-Model Router (§E7), scoped to this agent's model preference."""
        rendered = await self.tools.invoke(
            "prompt_loader", self.permissions, {"template": prompt_template, "variables": variables}
        )
        ctx.record_tool_call()
        if not rendered.success:
            raise RuntimeError(f"Prompt render failed for '{prompt_template}': {rendered.error}")

        response = await self.model_router.complete(
            self.name, system_prompt=f"You are the {self.name} agent.", user_prompt=rendered.output
        )
        ctx.record_model_usage(response.model, response.tokens, response.cost_estimate)
        return response.text

    async def produce_artifact(
        self, ctx: AgentContext, name: str, artifact_type: str, content: str, extra_outputs: dict[str, Any] | None = None
    ) -> DomainResult:
        """Persists the artifact via the Artifact Store tool (§14.1/§8) and returns
        the DomainResult the ReasoningPipeline needs to complete the task. Uses an
        idempotency key (Phase 1.5 §3) derived from (task, artifact name) so a
        retried Execute stage — e.g. after a transient failure mid-stage — resolves
        to the same artifact instead of a spurious extra version."""
        result = await self.tools.invoke(
            "artifact_store",
            self.permissions,
            {
                "workspaceId": str(ctx.workspace_id),
                "name": name,
                "type": artifact_type,
                "ownerAgent": self.name,
                "content": content,
                "workflowRunId": str(ctx.workflow_run_id),
                "taskNodeId": str(ctx.task_node_id),
                "correlationId": str(ctx.correlation_id),
                "idempotencyKey": f"{ctx.task_node_id}:{name}",
            },
        )
        ctx.record_tool_call()
        if not result.success:
            raise RuntimeError(f"Artifact store failed for '{name}': {result.error}")

        await self.memory.remember_working(
            ctx.task_node_id, f"Produced artifact '{name}': {content[:500]}", correlation_id=ctx.correlation_id
        )
        ctx.record_memory_write()

        return DomainResult(
            output_text=content,
            artifact_name=name,
            artifact_type=artifact_type,
            extra_outputs={"artifactId": result.output["artifactId"], **(extra_outputs or {})},
        )

    async def retry(self, fn: Callable[[], Awaitable[T]], attempts: int = 2, delay_seconds: float = 1.0) -> T:
        """Generic retry helper for sub-operations within Execute (e.g. a flaky
        tool call) — distinct from the .NET Scheduler's task-level retry (§10),
        which operates one level up, across whole task dispatches."""
        last_exc: Exception | None = None
        for attempt in range(1, attempts + 1):
            try:
                return await fn()
            except Exception as exc:  # noqa: BLE001
                last_exc = exc
                self.logger.warning(
                    "retrying operation", extra={"fields": {"agent": self.name, "attempt": attempt, "error": str(exc)}}
                )
                if attempt < attempts:
                    await asyncio.sleep(delay_seconds)
        assert last_exc is not None
        raise last_exc
