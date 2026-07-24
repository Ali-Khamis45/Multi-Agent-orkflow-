"""Python side of the Event Bus (ARCHITECTURE.md §6.1). Talks to the exact same
Redis stream ("workflow-events") and wire format (one field, "data", holding
the JSON-serialized EventEnvelope) as AiAgentsTeam.Infrastructure.EventBus.
RedisStreamsEventBus — this is the only coupling between the two runtimes.
"""

from __future__ import annotations

import asyncio
from collections.abc import Awaitable, Callable

import redis.asyncio as redis

from app.logging_config import get_logger
from app.models.event_envelope import EventEnvelope

logger = get_logger(__name__)

STREAM_KEY = "workflow-events"

Handler = Callable[[EventEnvelope], Awaitable[None]]


class RedisEventBus:
    def __init__(self, redis_url: str) -> None:
        self._client = redis.from_url(redis_url, decode_responses=True)

    async def publish(self, envelope: EventEnvelope) -> None:
        data = envelope.model_dump_json(by_alias=True)
        await self._client.xadd(STREAM_KEY, {"data": data})
        logger.debug("published event", extra={"fields": {"type": envelope.type, "event_id": str(envelope.event_id)}})

    async def ensure_group(self, group: str) -> None:
        try:
            await self._client.xgroup_create(STREAM_KEY, group, id="$", mkstream=True)
        except redis.ResponseError as exc:
            if "BUSYGROUP" not in str(exc):
                raise

    async def subscribe(
        self,
        group: str,
        consumer: str,
        handler: Handler,
        stop_event: asyncio.Event | None = None,
    ) -> None:
        """Runs until `stop_event` is set — intended to be launched as a background task."""
        await self.ensure_group(group)

        while stop_event is None or not stop_event.is_set():
            try:
                response = await self._client.xreadgroup(
                    groupname=group, consumername=consumer, streams={STREAM_KEY: ">"}, count=10, block=1000
                )
            except Exception:
                logger.exception("redis streams read failed; retrying shortly")
                await asyncio.sleep(2)
                continue

            if not response:
                continue

            for _stream_name, entries in response:
                for entry_id, fields in entries:
                    await self._process_entry(group, entry_id, fields, handler)

    async def _process_entry(self, group: str, entry_id: str, fields: dict, handler: Handler) -> None:
        try:
            envelope = EventEnvelope.model_validate_json(fields["data"])
            await handler(envelope)
            await self._client.xack(STREAM_KEY, group, entry_id)
        except Exception:
            # §10 Self-Healing philosophy applied to the bus itself: a bad event
            # never crashes the consumer loop; left un-ACKed for later inspection.
            logger.exception("failed to process event", extra={"fields": {"entry_id": entry_id, "group": group}})

    async def aclose(self) -> None:
        await self._client.aclose()
