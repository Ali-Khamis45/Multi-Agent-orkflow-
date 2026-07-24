"""Reasoning Engine (ARCHITECTURE_EXTENSION.md §E6, Phase 1.5 §1/§4) — the
12-stage sequence, per-stage telemetry, and success/failure event publishing —
exercised with fakes standing in for the API client, memory, and model router
(no network, no Postgres, no Redis: pure pipeline-logic unit tests)."""

from __future__ import annotations

import json
import uuid
from dataclasses import dataclass, field

import pytest

from app.models.event_envelope import EventTypes
from app.reasoning.pipeline import AgentContext, DomainResult, ReasoningPipeline


@dataclass
class FakeModelResponse:
    text: str
    model: str = "fake-model"
    provider: str = "fake"
    tokens: int = 10
    cost_estimate: float = 0.001
    fallback_triggered: bool = False


class FakeModelRouter:
    def __init__(self, critique_text: str = "Looks solid.") -> None:
        self.calls: list[tuple[str, str, str]] = []
        self._critique_text = critique_text

    async def complete(self, agent_capability, system_prompt, user_prompt):
        self.calls.append((agent_capability, system_prompt, user_prompt))
        return FakeModelResponse(text=self._critique_text)


class FakeMemoryClient:
    async def recall_working(self, task_id):
        return [{"content": "prior working memory"}]

    async def recall_project(self):
        return []


class FakeApiClient:
    def __init__(self) -> None:
        self.traces: list[dict] = []

    async def get_latest_artifact_by_name(self, workflow_run_id, name):
        return None

    async def record_reasoning_trace(self, **kwargs):
        self.traces.append(kwargs)


class FakeEventBus:
    def __init__(self) -> None:
        self.published: list = []

    async def publish(self, envelope):
        self.published.append(envelope)


def make_ctx(**overrides) -> AgentContext:
    defaults = dict(
        task_node_id=uuid.uuid4(),
        workflow_run_id=uuid.uuid4(),
        workspace_id=uuid.uuid4(),
        correlation_id=uuid.uuid4(),
        task_type="TestTask",
        name="TestNode",
        inputs={"goal": "test"},
    )
    defaults.update(overrides)
    return AgentContext(**defaults)


def make_pipeline(model_router=None):
    api = FakeApiClient()
    event_bus = FakeEventBus()
    pipeline = ReasoningPipeline(
        agent_name="test-agent",
        agent_capability="test-agent",
        tools_available=["prompt_loader", "artifact_store"],
        api=api,
        memory=FakeMemoryClient(),
        tools=object(),  # unused directly by the pipeline
        model_router=model_router or FakeModelRouter(),
        event_bus=event_bus,
    )
    return pipeline, api, event_bus


class TestReasoningPipelineSuccess:
    async def test_runs_all_twelve_stages_in_order(self):
        pipeline, api, _ = make_pipeline()
        ctx = make_ctx()

        async def execute_fn(c: AgentContext) -> DomainResult:
            return DomainResult(output_text="produced output", artifact_name="Doc", artifact_type="Markdown")

        await pipeline.run(ctx, execute_fn)

        stages = [t["stage"] for t in api.traces]
        assert stages == [
            "Observe", "Understand", "Think", "Plan", "RetrieveContext", "RetrieveMemory",
            "SelectTools", "Execute", "Reflect", "SelfCritique", "ConfidenceEvaluation", "PublishResult",
        ]

    async def test_publishes_task_completed_with_confidence(self):
        pipeline, _, event_bus = make_pipeline()
        ctx = make_ctx()

        async def execute_fn(c: AgentContext) -> DomainResult:
            return DomainResult(output_text="ok", artifact_name="Doc", artifact_type="Markdown")

        await pipeline.run(ctx, execute_fn)

        assert len(event_bus.published) == 1
        envelope = event_bus.published[0]
        assert envelope.type == EventTypes.TASK_COMPLETED
        assert envelope.correlation_id == ctx.correlation_id
        assert envelope.confidence is not None

    async def test_retrieve_memory_stage_counts_two_reads(self):
        pipeline, api, _ = make_pipeline()
        ctx = make_ctx()

        async def execute_fn(c: AgentContext) -> DomainResult:
            return DomainResult(output_text="ok", artifact_name="Doc", artifact_type="Markdown")

        await pipeline.run(ctx, execute_fn)

        retrieve_memory_trace = next(t for t in api.traces if t["stage"] == "RetrieveMemory")
        assert retrieve_memory_trace["memory_reads"] == 2

    async def test_self_critique_records_model_usage(self):
        pipeline, api, _ = make_pipeline(model_router=FakeModelRouter(critique_text="Looks solid."))
        ctx = make_ctx()

        async def execute_fn(c: AgentContext) -> DomainResult:
            return DomainResult(output_text="ok", artifact_name="Doc", artifact_type="Markdown")

        await pipeline.run(ctx, execute_fn)

        critique_trace = next(t for t in api.traces if t["stage"] == "SelfCritique")
        assert critique_trace["model_used"] == "fake-model"
        assert critique_trace["tokens"] == 10

    async def test_low_confidence_when_critique_flags_risk(self):
        pipeline, _, event_bus = make_pipeline(
            model_router=FakeModelRouter(critique_text="This is uncertain and incomplete, a real risk.")
        )
        ctx = make_ctx()

        async def execute_fn(c: AgentContext) -> DomainResult:
            return DomainResult(output_text="ok", artifact_name="Doc", artifact_type="Markdown")

        await pipeline.run(ctx, execute_fn)

        envelope = event_bus.published[0]
        assert envelope.confidence < 0.85
        assert envelope.risk_level in ("medium", "high")


class TestReasoningPipelineFailure:
    async def test_execute_exception_publishes_structured_task_failed(self):
        pipeline, api, event_bus = make_pipeline()
        ctx = make_ctx()

        async def execute_fn(c: AgentContext) -> DomainResult:
            raise ValueError("domain logic exploded")

        await pipeline.run(ctx, execute_fn)

        assert len(event_bus.published) == 1
        envelope = event_bus.published[0]
        assert envelope.type == EventTypes.TASK_FAILED

        payload = json.loads(envelope.payload_json)
        reason = json.loads(payload["ReasonJson"])
        assert reason["category"] == "Unknown"
        assert reason["retryable"] is True

        # PublishResult is still traced even on the failure path.
        assert api.traces[-1]["stage"] == "PublishResult"

    async def test_failure_does_not_raise_out_of_run(self):
        pipeline, _, _ = make_pipeline()
        ctx = make_ctx()

        async def execute_fn(c: AgentContext) -> DomainResult:
            raise RuntimeError("boom")

        # Should not raise — the pipeline's failure boundary handles it.
        await pipeline.run(ctx, execute_fn)
