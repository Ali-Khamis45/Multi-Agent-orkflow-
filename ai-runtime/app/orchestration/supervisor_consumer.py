"""Feeds every bus event to the Supervisor's own consumer group ("supervisor")
— independent of the "ai-runtime-agents" group the agents themselves use, so a
slow Supervisor reaction can never delay a task dispatch, and vice versa
(same fan-out pattern as .NET's "orchestrator" vs "signalr-relay" groups).
"""

from __future__ import annotations

import asyncio

from app.clients.event_bus import RedisEventBus
from app.models.event_envelope import EventEnvelope
from app.supervisor.supervisor_agent import SupervisorAgent


class SupervisorEventConsumer:
    def __init__(self, event_bus: RedisEventBus, supervisor: SupervisorAgent, consumer_name: str) -> None:
        self._event_bus = event_bus
        self._supervisor = supervisor
        self._consumer_name = consumer_name

    async def run(self, stop_event: asyncio.Event) -> None:
        await self._event_bus.subscribe("supervisor", self._consumer_name, self._handle, stop_event)

    async def _handle(self, envelope: EventEnvelope) -> None:
        await self._supervisor.on_event(envelope)
