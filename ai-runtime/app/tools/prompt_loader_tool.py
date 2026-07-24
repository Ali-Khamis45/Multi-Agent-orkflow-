"""Prompt Loader tool (ARCHITECTURE.md §8 initial tool set). Renders a named,
versioned template via the Prompt Registry (Phase 1.5 §8) — this tool no
longer reads .txt files directly, so prompt versioning/rollback/history is
entirely a registry concern, invisible to callers.
"""

from __future__ import annotations

from typing import Any

from app.tools.base import Tool, ToolError, ToolMetadata, ToolResult
from app.tools.prompt_registry import PromptRegistry, PromptRegistryError


class PromptLoaderTool(Tool):
    def __init__(self, registry: PromptRegistry) -> None:
        self._registry = registry

    @property
    def metadata(self) -> ToolMetadata:
        return ToolMetadata(
            name="prompt_loader",
            description="Load and render a named, versioned prompt template with variables.",
            input_schema={
                "required": ["template"],
                "properties": {
                    "template": {"type": "string"},
                    "variables": {"type": "object"},
                    "version": {"type": "integer"},
                },
            },
            required_permission=None,
        )

    async def execute(self, params: dict[str, Any]) -> ToolResult:
        try:
            rendered = self._registry.render(
                params["template"], params.get("variables", {}), params.get("version")
            )
        except PromptRegistryError as exc:
            raise ToolError(str(exc)) from exc

        return ToolResult(success=True, output=rendered)
