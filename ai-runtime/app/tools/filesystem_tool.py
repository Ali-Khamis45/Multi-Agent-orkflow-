"""Filesystem tool (ARCHITECTURE.md §8 initial tool set). Sandboxed to one root
directory per workspace so an agent can never traverse outside its project's
working copy — the concrete enforcement of the `filesystem` permission scope.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any

from app.tools.base import Tool, ToolError, ToolMetadata, ToolResult


class FilesystemTool(Tool):
    def __init__(self, root: Path) -> None:
        self._root = root
        self._root.mkdir(parents=True, exist_ok=True)

    @property
    def metadata(self) -> ToolMetadata:
        return ToolMetadata(
            name="filesystem",
            description="Read/write files within the agent's sandboxed workspace directory.",
            input_schema={
                "required": ["operation", "path"],
                "properties": {
                    "operation": {"enum": ["read", "write", "list"]},
                    "path": {"type": "string"},
                    "content": {"type": "string"},
                },
            },
            required_permission="filesystem",
        )

    def _resolve(self, relative_path: str) -> Path:
        target = (self._root / relative_path).resolve()
        if self._root not in target.parents and target != self._root:
            raise ToolError(f"Path '{relative_path}' escapes the sandboxed workspace root.")
        return target

    async def execute(self, params: dict[str, Any]) -> ToolResult:
        operation = params["operation"]
        path = self._resolve(params["path"])

        if operation == "write":
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(params.get("content", ""), encoding="utf-8")
            return ToolResult(success=True, output={"bytesWritten": len(params.get("content", ""))})

        if operation == "read":
            if not path.exists():
                return ToolResult(success=False, error=f"File not found: {params['path']}")
            return ToolResult(success=True, output=path.read_text(encoding="utf-8"))

        if operation == "list":
            if not path.exists():
                return ToolResult(success=True, output=[])
            return ToolResult(success=True, output=[p.name for p in path.iterdir()])

        return ToolResult(success=False, error=f"Unsupported operation '{operation}'")
