"""Connector Action tool (Phase 4 "AI Company Operating System" — Connector
Framework). Lets an agent perform a real action through an installed connector
(post to Instagram, commit a file to GitHub, ...) the same way it already
persists an artifact through the artifact_store tool — one more tool call,
not a bespoke HTTP client per agent.

Deliberately checks installation status first rather than letting the API
call fail: a connector being unconnected is an expected, common state (most
workspaces won't have every connector installed), not an error condition an
agent should treat as a tool failure worth logging loudly.
"""

from __future__ import annotations

import uuid
from typing import Any

from app.clients.api_client import ApiClient
from app.tools.base import Tool, ToolMetadata, ToolResult


class ConnectorActionTool(Tool):
    def __init__(self, api: ApiClient) -> None:
        self._api = api

    @property
    def metadata(self) -> ToolMetadata:
        return ToolMetadata(
            name="connector_action",
            description="Perform a real action through an installed external connector (Shopify, GitHub, Meta, ...).",
            input_schema={
                "required": ["workspaceId", "connectorKey", "actionKey", "inputJson"],
                "properties": {
                    "workspaceId": {"type": "string"},
                    "connectorKey": {"type": "string"},
                    "actionKey": {"type": "string"},
                    "inputJson": {"type": "string"},
                },
            },
            required_permission="connector_action",
        )

    async def execute(self, params: dict[str, Any]) -> ToolResult:
        workspace_id = uuid.UUID(params["workspaceId"])
        connector_key = params["connectorKey"]

        installed = await self._api.get_installed_connectors(workspace_id)
        if not any(c["connectorKey"] == connector_key and c["status"] == "Connected" for c in installed):
            return ToolResult(
                success=False,
                error=f"Connector '{connector_key}' is not connected for this workspace — skipping the action.",
            )

        result = await self._api.execute_connector_action(
            workspace_id, connector_key, params["actionKey"], params["inputJson"]
        )
        return ToolResult(success=bool(result.get("success")), output=result, error=result.get("errorMessage"))
