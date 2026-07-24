"""Artifact Store tool (ARCHITECTURE.md §8 initial tool set / §14.1 Artifact
Manager). Wraps the ApiClient so agents produce artifacts uniformly through
tool-calling rather than each agent embedding its own HTTP call.
"""

from __future__ import annotations
from typing import Any

from app.clients.api_client import ApiClient
from app.tools.base import Tool, ToolMetadata, ToolResult


class ArtifactStoreTool(Tool):
    def __init__(self, api: ApiClient) -> None:
        self._api = api

    @property
    def metadata(self) -> ToolMetadata:
        return ToolMetadata(
            name="artifact_store",
            description="Persist a produced artifact (code, markdown, json, ...) with versioning.",
            input_schema={
                "required": ["workspaceId", "name", "type", "ownerAgent", "content"],
                "properties": {
                    "workspaceId": {"type": "string"},
                    "name": {"type": "string"},
                    "type": {"enum": ["Code", "Markdown", "Json", "Test", "Dockerfile", "Sql", "Image", "Diagram"]},
                    "ownerAgent": {"type": "string"},
                    "content": {"type": "string"},
                    "workflowRunId": {"type": "string"},
                    "taskNodeId": {"type": "string"},
                },
            },
            required_permission="artifact_store",
        )

    async def execute(self, params: dict[str, Any]) -> ToolResult:
        import uuid

        artifact_id = await self._api.create_artifact(
            workspace_id=uuid.UUID(params["workspaceId"]),
            name=params["name"],
            artifact_type=params["type"],
            owner_agent=params["ownerAgent"],
            content=params["content"],
            workflow_run_id=uuid.UUID(params["workflowRunId"]) if params.get("workflowRunId") else None,
            task_node_id=uuid.UUID(params["taskNodeId"]) if params.get("taskNodeId") else None,
        )
        return ToolResult(success=True, output={"artifactId": str(artifact_id)})
