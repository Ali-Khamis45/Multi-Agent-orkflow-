"""Tool Marketplace registry (ARCHITECTURE.md §8). Agents request tools by
name at execution time; access is granted only if the requesting agent's
manifest permissions (§4.1) include the tool's required permission — the
same enforcement point ARCHITECTURE.md describes, just implemented here in
the AI Runtime rather than a separate service for Phase 1.
"""

from __future__ import annotations

from app.logging_config import get_logger
from app.tools.base import Tool, ToolError, ToolResult

logger = get_logger(__name__)


class ToolRegistry:
    def __init__(self) -> None:
        self._tools: dict[str, Tool] = {}

    def register(self, tool: Tool) -> None:
        self._tools[tool.metadata.name] = tool

    def list_tools(self) -> list[str]:
        return list(self._tools.keys())

    async def invoke(self, tool_name: str, agent_permissions: list[str], params: dict) -> ToolResult:
        tool = self._tools.get(tool_name)
        if tool is None:
            return ToolResult(success=False, error=f"Unknown tool '{tool_name}'")

        required = tool.metadata.required_permission
        if required is not None and required not in agent_permissions:
            logger.warning(
                "tool access denied", extra={"fields": {"tool": tool_name, "required_permission": required}}
            )
            return ToolResult(success=False, error=f"Permission '{required}' not granted for tool '{tool_name}'")

        try:
            tool.validate(params)
            return await tool.execute(params)
        except ToolError as exc:
            return ToolResult(success=False, error=str(exc))
