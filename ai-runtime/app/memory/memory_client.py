"""Multi-Layer Memory client (ARCHITECTURE_EXTENSION.md §E5). Phase 1 wires up
Working, Conversation, and Project; the interface already accepts any layer
name, so Vector/LongTerm retrieval (Phase 3, per ARCHITECTURE.md §24) plugs in
without changing a single caller.

All persistence goes through the ApiClient — this class never touches a
database (ARCHITECTURE.md §2 rule for this runtime).
"""

from __future__ import annotations

import uuid
from typing import Any

from app.clients.api_client import ApiClient


class MemoryLayer:
    WORKING = "Working"
    CONVERSATION = "Conversation"
    WORKFLOW = "Workflow"
    PROJECT = "Project"
    LONG_TERM = "LongTerm"


class MemoryKind:
    REQUIREMENT = "Requirement"
    ARCHITECTURE = "Architecture"
    CODE = "Code"
    DOC = "Doc"
    DECISION = "Decision"
    LESSON_LEARNED = "LessonLearned"


class MemoryClient:
    def __init__(self, api: ApiClient, workspace_id: uuid.UUID) -> None:
        self._api = api
        self._workspace_id = workspace_id

    async def remember(self, layer: str, scope_ref: uuid.UUID, kind: str, content: str) -> uuid.UUID:
        return await self._api.write_memory(self._workspace_id, layer, scope_ref, kind, content)

    async def recall(self, layer: str, scope_ref: uuid.UUID, limit: int = 20) -> list[dict[str, Any]]:
        return await self._api.query_memory(self._workspace_id, layer, scope_ref, limit)

    # Convenience wrappers matching AgentBase's usage (task-scoped working memory,
    # workflow-scoped conversation, workspace-scoped project memory).
    async def remember_working(self, task_id: uuid.UUID, content: str, kind: str = MemoryKind.DECISION) -> None:
        await self.remember(MemoryLayer.WORKING, task_id, kind, content)

    async def recall_working(self, task_id: uuid.UUID) -> list[dict[str, Any]]:
        return await self.recall(MemoryLayer.WORKING, task_id)

    async def remember_conversation(self, workflow_run_id: uuid.UUID, content: str) -> None:
        await self.remember(MemoryLayer.CONVERSATION, workflow_run_id, MemoryKind.DECISION, content)

    async def recall_conversation(self, workflow_run_id: uuid.UUID) -> list[dict[str, Any]]:
        return await self.recall(MemoryLayer.CONVERSATION, workflow_run_id)

    async def remember_project(self, content: str, kind: str = MemoryKind.DECISION) -> None:
        await self.remember(MemoryLayer.PROJECT, self._workspace_id, kind, content)

    async def recall_project(self) -> list[dict[str, Any]]:
        return await self.recall(MemoryLayer.PROJECT, self._workspace_id)
