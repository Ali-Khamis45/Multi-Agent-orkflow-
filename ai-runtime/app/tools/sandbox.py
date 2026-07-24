"""Shared sandboxing primitives (Phase 1.5 §7 Tool Sandbox). Any tool that
touches the filesystem — today's FilesystemTool, and any future tool a
plugin (§7 Plugin System) registers — should resolve paths through
`resolve_sandboxed_path` rather than rolling its own containment check, so
the security model is enforced identically everywhere.

Defenses:
  - rejects absolute input paths outright (a caller must always pass a path
    relative to its sandbox root)
  - rejects any ".." path segment before resolution, not only after (defense
    in depth against a resolver behaving unexpectedly across platforms)
  - resolves symlinks (Path.resolve()) and re-checks containment against the
    resolved root, so a symlink planted inside the sandbox cannot point
    outside of it
  - enforces a maximum content size for writes, so a tool can't be used to
    exhaust disk space
"""

from __future__ import annotations

from pathlib import Path
from typing import Final

from app.tools.base import ToolError

DEFAULT_MAX_BYTES: Final[int] = 1_000_000


def resolve_sandboxed_path(root: Path, relative_path: str) -> Path:
    if not relative_path or relative_path.strip() == "":
        raise ToolError("Path must not be empty.")

    candidate = Path(relative_path)
    if candidate.is_absolute():
        raise ToolError(f"Path '{relative_path}' must be relative to the sandbox root, not absolute.")

    if ".." in candidate.parts:
        raise ToolError(f"Path '{relative_path}' must not contain '..' segments.")

    resolved_root = root.resolve()
    target = (resolved_root / candidate).resolve()

    if target != resolved_root and resolved_root not in target.parents:
        raise ToolError(f"Path '{relative_path}' escapes the sandboxed root.")

    return target


def enforce_max_size(content: str, max_bytes: int) -> None:
    size = len(content.encode("utf-8"))
    if size > max_bytes:
        raise ToolError(f"Content size {size} bytes exceeds the sandbox limit of {max_bytes} bytes.")
