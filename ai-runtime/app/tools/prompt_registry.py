"""Prompt Registry (Phase 1.5 §8) — a centralized, versioned catalog of every
prompt template, backed by prompts/registry.json + the .txt template files
alongside it. Each entry exposes version/description/variables/owner/
compatible agent/history, so future prompt optimization (a new version, an
A/B test, a rollback) is a JSON + file change — never a code change.

A DB-backed `prompt_templates` table with full experiment tracking
(ARCHITECTURE.md §14.2) is a Phase 2+ upgrade; this file-based registry is
the Phase 1.5 implementation of the same contract, so callers (PromptLoaderTool)
don't change when that upgrade happens.
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


class PromptRegistryError(Exception):
    pass


@dataclass(frozen=True)
class PromptVersion:
    version: int
    file: str
    description: str
    variables: list[str]
    created_at: str


@dataclass(frozen=True)
class PromptEntry:
    name: str
    owner: str
    compatible_agent: str
    current_version: int
    versions: list[PromptVersion]

    def version(self, version: int | None = None) -> PromptVersion:
        target = version or self.current_version
        for v in self.versions:
            if v.version == target:
                return v
        raise PromptRegistryError(f"Prompt '{self.name}' has no version {target}.")


class PromptRegistry:
    def __init__(self, prompts_dir: Path) -> None:
        self._dir = prompts_dir
        self._entries = self._load()

    def _load(self) -> dict[str, PromptEntry]:
        manifest_path = self._dir / "registry.json"
        raw = json.loads(manifest_path.read_text(encoding="utf-8"))
        entries: dict[str, PromptEntry] = {}
        for name, spec in raw.items():
            versions = [
                PromptVersion(v["version"], v["file"], v["description"], v["variables"], v["created_at"])
                for v in spec["versions"]
            ]
            entries[name] = PromptEntry(
                name=name,
                owner=spec["owner"],
                compatible_agent=spec["compatible_agent"],
                current_version=spec["current_version"],
                versions=versions,
            )
        return entries

    def get_entry(self, name: str) -> PromptEntry:
        if name not in self._entries:
            raise PromptRegistryError(f"Unknown prompt template '{name}'.")
        return self._entries[name]

    def render(self, name: str, variables: dict, version: int | None = None) -> str:
        entry = self.get_entry(name)
        prompt_version = entry.version(version)
        template = (self._dir / prompt_version.file).read_text(encoding="utf-8")
        try:
            return template.format(**variables)
        except KeyError as exc:
            raise PromptRegistryError(f"Prompt '{name}' v{prompt_version.version} missing variable {exc}.") from exc

    def list_entries(self) -> list[PromptEntry]:
        return list(self._entries.values())
