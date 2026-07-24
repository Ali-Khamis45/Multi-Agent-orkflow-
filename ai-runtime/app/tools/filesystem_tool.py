"""Filesystem tool (ARCHITECTURE.md §8 initial tool set). Sandboxed to one root
directory per workspace so an agent can never traverse outside its project's
working copy. Path containment and size limits are enforced by the shared
`app.tools.sandbox` module (Phase 1.5 §7) so future filesystem-adjacent tools
inherit the identical security model rather than reimplementing it.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any

from app.tools.base import Tool, ToolError, ToolMetadata, ToolResult
from app.tools.sandbox import DEFAULT_MAX_BYTES, enforce_max_size, resolve_sandboxed_path


class FilesystemTool(Tool):
    def __init__(self, root: Path, max_bytes: int = DEFAULT_MAX_BYTES) -> None:
        self._root = root
        self._max_bytes = max_bytes
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

    async def execute(self, params: dict[str, Any]) -> ToolResult:
        operation = params["operation"]
        path = resolve_sandboxed_path(self._root, params["path"])

        if operation == "write":
            content = params.get("content", "")
            enforce_max_size(content, self._max_bytes)
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(content, encoding="utf-8")
            return ToolResult(success=True, output={"bytesWritten": len(content.encode("utf-8"))})

        if operation == "read":
            if not path.exists():
                return ToolResult(success=False, error=f"File not found: {params['path']}")
            return ToolResult(success=True, output=path.read_text(encoding="utf-8"))

        if operation == "list":
            if not path.exists():
                return ToolResult(success=True, output=[])
            return ToolResult(success=True, output=[p.name for p in path.iterdir()])

        return ToolResult(success=False, error=f"Unsupported operation '{operation}'")
