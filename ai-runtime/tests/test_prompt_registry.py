"""Prompt Registry (Phase 1.5 §8) — versioned templates with metadata, and
rendering that fails loudly on a missing variable rather than silently."""

import json

import pytest

from app.tools.prompt_registry import PromptRegistry, PromptRegistryError


@pytest.fixture
def registry_dir(tmp_path):
    manifest = {
        "greeting": {
            "current_version": 2,
            "owner": "platform",
            "compatible_agent": "business-analyst",
            "versions": [
                {
                    "version": 1,
                    "file": "greeting_v1.txt",
                    "description": "v1",
                    "variables": ["name"],
                    "created_at": "2026-01-01",
                },
                {
                    "version": 2,
                    "file": "greeting_v2.txt",
                    "description": "v2, adds a farewell",
                    "variables": ["name", "farewell"],
                    "created_at": "2026-02-01",
                },
            ],
        }
    }
    (tmp_path / "registry.json").write_text(json.dumps(manifest))
    (tmp_path / "greeting_v1.txt").write_text("Hello, {name}!")
    (tmp_path / "greeting_v2.txt").write_text("Hello, {name}! {farewell}")
    return tmp_path


class TestPromptRegistry:
    def test_renders_the_current_version_by_default(self, registry_dir):
        registry = PromptRegistry(registry_dir)
        rendered = registry.render("greeting", {"name": "Ada", "farewell": "Goodbye"})
        assert rendered == "Hello, Ada! Goodbye"

    def test_can_render_an_older_version_explicitly(self, registry_dir):
        registry = PromptRegistry(registry_dir)
        rendered = registry.render("greeting", {"name": "Ada"}, version=1)
        assert rendered == "Hello, Ada!"

    def test_missing_variable_raises_a_clear_error(self, registry_dir):
        registry = PromptRegistry(registry_dir)
        with pytest.raises(PromptRegistryError, match="farewell"):
            registry.render("greeting", {"name": "Ada"})  # v2 needs farewell too

    def test_unknown_template_raises(self, registry_dir):
        registry = PromptRegistry(registry_dir)
        with pytest.raises(PromptRegistryError, match="Unknown"):
            registry.render("does_not_exist", {})

    def test_unknown_version_raises(self, registry_dir):
        registry = PromptRegistry(registry_dir)
        with pytest.raises(PromptRegistryError, match="version"):
            registry.get_entry("greeting").version(99)

    def test_entry_exposes_full_metadata(self, registry_dir):
        registry = PromptRegistry(registry_dir)
        entry = registry.get_entry("greeting")
        assert entry.owner == "platform"
        assert entry.compatible_agent == "business-analyst"
        assert entry.current_version == 2
        assert len(entry.versions) == 2
        assert entry.versions[0].description == "v1"
