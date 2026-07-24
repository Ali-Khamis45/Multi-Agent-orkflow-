"""Tool plugin contract (ARCHITECTURE.md §8 Tool Marketplace). Every tool —
built-in or added later by a plugin (§7) — exposes the same five things:
metadata, a JSON-schema-shaped input contract, a required permission string,
input validation, and execution. Adding a new tool never requires touching
the ToolRegistry or any agent; it only requires implementing this interface
and registering an instance.
"""

from __future__ import annotations

from abc import ABC, abstractmethod
from dataclasses import dataclass, field
from typing import Any


class ToolError(Exception):
    """Raised for validation failures or permission denials — never a bare exception."""


@dataclass(frozen=True)
class ToolMetadata:
    name: str
    description: str
    input_schema: dict[str, Any]
    required_permission: str | None = None


@dataclass
class ToolResult:
    success: bool
    output: Any = None
    error: str | None = None
    metadata: dict[str, Any] = field(default_factory=dict)


class Tool(ABC):
    @property
    @abstractmethod
    def metadata(self) -> ToolMetadata: ...

    def validate(self, params: dict[str, Any]) -> None:
        """Default validation: every schema key without a default is required.
        Concrete tools may override for richer checks."""
        required = self.metadata.input_schema.get("required", [])
        missing = [key for key in required if key not in params]
        if missing:
            raise ToolError(f"Missing required parameters for tool '{self.metadata.name}': {missing}")

    @abstractmethod
    async def execute(self, params: dict[str, Any]) -> ToolResult: ...
