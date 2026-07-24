"""Routes TaskDispatched events (published by the .NET Scheduler, §5.2) to the
matching local agent instance by name, and runs each as an independent asyncio
task — this is what lets Backend Engineer and Frontend Engineer actually
execute concurrently (ARCHITECTURE.md §5.2 step 3 / §E1) rather than one
blocking the other inside a single consumer loop.
"""

from __future__ import annotations

import asyncio
import json

from app.agents.base import AgentBase
from app.clients.event_bus import RedisEventBus
from app.logging_config import get_logger
from app.models.event_envelope import EventEnvelope, EventTypes

logger = get_logger(__name__)


class AgentEventConsumer:
    def __init__(self, event_bus: RedisEventBus, agents: dict[str, AgentBase], group: str, consumer: str) -> None:
        self._event_bus = event_bus
        self._agents = agents
        self._group = group
        self._consumer = consumer
        self._in_flight: set[asyncio.Task] = set()

    async def run(self, stop_event: asyncio.Event) -> None:
        await self._event_bus.subscribe(self._group, self._consumer, self._handle, stop_event)

    async def _handle(self, envelope: EventEnvelope) -> None:
        if envelope.type != EventTypes.TASK_DISPATCHED:
            return

        payload = json.loads(envelope.payload_json)
        assigned_agent = payload.get("AssignedAgent")
        agent = self._agents.get(assigned_agent)

        if agent is None:
            logger.warning("no local agent for dispatch", extra={"fields": {"assignedAgent": assigned_agent}})
            return

        task = asyncio.create_task(agent.handle_task_dispatched(envelope))
        self._in_flight.add(task)
        task.add_done_callback(self._in_flight.discard)
