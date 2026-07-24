"""Prompt Loader tool (ARCHITECTURE.md §8 initial tool set). Loads a named
template from the prompts/ directory and renders it with the given variables.
Backs Prompt Versioning (§14.2) in spirit for Phase 1: templates are files
under version control; a `prompt_templates` DB table with real version/rollback
is a Phase 2+ item (build order §24) that this tool's interface will slot
into without callers changing.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any

from app.tools.base import Tool, ToolError, ToolMetadata, ToolResult


class PromptLoaderTool(Tool):
    def __init__(self, prompts_dir: Path) -> None:
        self._prompts_dir = prompts_dir

    @property
    def metadata(self) -> ToolMetadata:
        return ToolMetadata(
            name="prompt_loader",
            description="Load and render a named prompt template with variables.",
            input_schema={
                "required": ["template"],
                "properties": {"template": {"type": "string"}, "variables": {"type": "object"}},
            },
            required_permission=None,
        )

    async def execute(self, params: dict[str, Any]) -> ToolResult:
        template_name = params["template"]
        variables = params.get("variables", {})
        path = self._prompts_dir / f"{template_name}.txt"

        if not path.exists():
            raise ToolError(f"Prompt template '{template_name}' not found.")

        template = path.read_text(encoding="utf-8")
        try:
            rendered = template.format(**variables)
        except KeyError as exc:
            raise ToolError(f"Prompt template '{template_name}' missing variable {exc}.") from exc

        return ToolResult(success=True, output=rendered)
