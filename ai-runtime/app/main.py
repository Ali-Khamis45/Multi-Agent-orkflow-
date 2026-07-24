"""AI Runtime entrypoint. Wires together the API/event-bus clients, tools,
model router, the seven Phase 1 agents, the Intent Engine, and the Supervisor
Brain, then exposes a minimal FastAPI surface: POST /intake is the "submit a
request and watch it flow through the whole pipeline" entry point from this
milestone's success criteria.
"""

from __future__ import annotations

import asyncio
import uuid
from contextlib import asynccontextmanager
from pathlib import Path

import httpx
from fastapi import FastAPI
from pydantic import BaseModel

from app.agents.backend_engineer import BackendEngineerAgent
from app.agents.base import AgentBase
from app.agents.business_analyst import BusinessAnalystAgent
from app.agents.code_reviewer import CodeReviewerAgent
from app.agents.frontend_engineer import FrontendEngineerAgent
from app.agents.project_manager import ProjectManagerAgent
from app.agents.qa_engineer import QaEngineerAgent
from app.agents.system_architect import SystemArchitectAgent
from app.clients.api_client import ApiClient
from app.clients.event_bus import RedisEventBus
from app.config import get_settings
from app.intent.intent_engine import IntentEngine
from app.logging_config import configure_logging, get_logger
from app.memory.memory_client import MemoryClient
from app.orchestration.event_consumer import AgentEventConsumer
from app.orchestration.supervisor_consumer import SupervisorEventConsumer
from app.routing.model_router import ModelRouter
from app.supervisor.supervisor_agent import SupervisorAgent
from app.tools.artifact_store_tool import ArtifactStoreTool
from app.tools.filesystem_tool import FilesystemTool
from app.tools.prompt_loader_tool import PromptLoaderTool
from app.tools.prompt_registry import PromptRegistry
from app.tools.registry import ToolRegistry

logger = get_logger(__name__)

AGENT_CLASSES: list[type[AgentBase]] = [
    BusinessAnalystAgent,
    ProjectManagerAgent,
    SystemArchitectAgent,
    BackendEngineerAgent,
    FrontendEngineerAgent,
    CodeReviewerAgent,
    QaEngineerAgent,
]


class Runtime:
    """Holds every long-lived object constructed at startup — attached to
    app.state so request handlers (and tests) can reach them."""

    def __init__(self) -> None:
        settings = get_settings()
        self.settings = settings
        self.api = ApiClient(settings.api_base_url)
        self.event_bus = RedisEventBus(settings.redis_url)
        self.model_router = ModelRouter(settings)

        self.prompt_registry = PromptRegistry(Path(__file__).parent / "prompts")
        self.tools = ToolRegistry()
        self.tools.register(FilesystemTool(root=Path(settings.workspace_files_root), max_bytes=settings.filesystem_max_bytes))
        self.tools.register(PromptLoaderTool(registry=self.prompt_registry))
        self.tools.register(ArtifactStoreTool(self.api))

        self.default_workspace_id: uuid.UUID | None = None
        self.memory: MemoryClient | None = None
        self.agents: dict[str, AgentBase] = {}
        self.intent_engine: IntentEngine | None = None
        self.supervisor: SupervisorAgent | None = None
        self._stop_event = asyncio.Event()
        self._background_tasks: list[asyncio.Task] = []

    async def start(self) -> None:
        self.default_workspace_id = await self._create_default_workspace_with_retry()
        self.memory = MemoryClient(self.api, self.default_workspace_id)
        logger.info("default workspace ready", extra={"fields": {"workspaceId": str(self.default_workspace_id)}})

        for agent_cls in AGENT_CLASSES:
            agent = agent_cls(
                api=self.api,
                memory=self.memory,
                tools=self.tools,
                event_bus=self.event_bus,
                endpoint=f"{self.settings.self_base_url}/agents/{agent_cls.name}",
                model_router=self.model_router,
            )
            await agent.register(self.default_workspace_id)
            self.agents[agent.name] = agent
            self._background_tasks.append(
                asyncio.create_task(agent.heartbeat_loop(self.settings.heartbeat_interval_seconds, self._stop_event))
            )

        self.intent_engine = IntentEngine(self.api, self.model_router, self.tools)
        self.supervisor = SupervisorAgent(self.api, self.intent_engine)

        agent_consumer = AgentEventConsumer(
            self.event_bus, self.agents, self.settings.consumer_group, self.settings.consumer_name
        )
        supervisor_consumer = SupervisorEventConsumer(self.event_bus, self.supervisor, self.settings.consumer_name)

        self._background_tasks.append(asyncio.create_task(agent_consumer.run(self._stop_event)))
        self._background_tasks.append(asyncio.create_task(supervisor_consumer.run(self._stop_event)))
        logger.info("AI Runtime started", extra={"fields": {"agents": list(self.agents.keys())}})

    async def _create_default_workspace_with_retry(self, attempts: int = 15, delay_seconds: float = 2.0) -> uuid.UUID:
        """docker-compose's plain `depends_on` waits for the api container to
        start, not for ASP.NET Core + its startup migration to finish — so the
        first call here can legitimately fail for a few seconds on a fresh stack."""
        last_exc: Exception | None = None
        for attempt in range(1, attempts + 1):
            try:
                return await self.api.create_workspace("default")
            except (httpx.ConnectError, httpx.HTTPStatusError) as exc:
                last_exc = exc
                logger.warning(
                    "API not ready yet; retrying", extra={"fields": {"attempt": attempt, "error": str(exc)}}
                )
                await asyncio.sleep(delay_seconds)
        assert last_exc is not None
        raise last_exc

    async def stop(self) -> None:
        self._stop_event.set()
        for task in self._background_tasks:
            task.cancel()
        await asyncio.gather(*self._background_tasks, return_exceptions=True)
        await self.api.aclose()
        await self.event_bus.aclose()
        await self.model_router.aclose()


@asynccontextmanager
async def lifespan(app: FastAPI):
    configure_logging(get_settings().log_level)
    runtime = Runtime()
    await runtime.start()
    app.state.runtime = runtime
    yield
    await runtime.stop()


app = FastAPI(title="AI Agents Team — AI Runtime", lifespan=lifespan)


class IntakeRequest(BaseModel):
    raw_input: str
    workspace_id: uuid.UUID | None = None


class IntakeResponse(BaseModel):
    workflow_run_id: uuid.UUID


@app.post("/intake", response_model=IntakeResponse)
async def intake(request: IntakeRequest) -> IntakeResponse:
    """User Request -> Intent Engine -> Business Analyst -> Supervisor Brain ->
    Project Manager -> System Architect -> Backend + Frontend (parallel) ->
    Code Reviewer -> QA Engineer -> Final structured artifacts."""
    runtime: Runtime = app.state.runtime
    workspace_id = request.workspace_id or runtime.default_workspace_id
    assert workspace_id is not None
    assert runtime.supervisor is not None

    run_id = await runtime.supervisor.kickoff(workspace_id, request.raw_input)
    return IntakeResponse(workflow_run_id=run_id)


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/prompts")
async def list_prompts() -> list[dict]:
    """Prompt Registry (§8), read by the .NET API's proxy endpoint
    (GET /api/prompts) — the dashboard never calls this runtime directly
    (Phase 1.6 design goal); only the ASP.NET API does, server-to-server."""
    runtime: Runtime = app.state.runtime
    return [
        {
            "name": entry.name,
            "owner": entry.owner,
            "compatibleAgent": entry.compatible_agent,
            "currentVersion": entry.current_version,
            "versions": [
                {
                    "version": v.version,
                    "file": v.file,
                    "description": v.description,
                    "variables": v.variables,
                    "createdAt": v.created_at,
                }
                for v in entry.versions
            ],
        }
        for entry in runtime.prompt_registry.list_entries()
    ]


@app.get("/agents")
async def list_agents() -> dict[str, dict]:
    runtime: Runtime = app.state.runtime
    return {name: {"metrics": agent.metrics, "manifest": agent.manifest()} for name, agent in runtime.agents.items()}
