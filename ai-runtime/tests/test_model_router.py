"""Multi-Model Router (ARCHITECTURE_EXTENSION.md §E7) — provider preference,
automatic fallback, and the deterministic mock when nothing is configured."""

import pytest

from app.config import Settings
from app.routing.model_router import ModelRouter


def _settings(**overrides) -> Settings:
    return Settings(**overrides)


class TestModelRouterMockFallback:
    async def test_uses_mock_when_no_provider_configured(self):
        router = ModelRouter(_settings())
        response = await router.complete("business-analyst", "system", "hello world")

        assert response.provider == "mock"
        assert "MOCK completion" in response.text
        assert response.fallback_triggered is False
        assert response.cost_estimate == 0.0
        await router.aclose()

    async def test_mock_response_includes_token_estimate(self):
        router = ModelRouter(_settings())
        response = await router.complete("qa-engineer", "system", "x" * 40)
        assert response.tokens > 0
        await router.aclose()


class TestModelRouterFallback:
    async def test_falls_back_to_next_provider_when_first_fails(self, monkeypatch):
        # backend-engineer's preference is [openai, anthropic, gemini, ollama] (§E7) —
        # fail the first-preferred provider so the router must fall back to the second.
        router = ModelRouter(_settings(anthropic_api_key="k1", openai_api_key="k2"))

        calls: list[str] = []

        async def fake_invoke(provider, system_prompt, user_prompt):
            calls.append(provider)
            if provider == "openai":
                raise RuntimeError("simulated provider outage")
            return "anthropic response", "claude-sonnet-5", 42

        monkeypatch.setattr(router, "_invoke", fake_invoke)

        response = await router.complete("backend-engineer", "system", "prompt")

        assert calls == ["openai", "anthropic"]
        assert response.provider == "anthropic"
        assert response.fallback_triggered is True
        assert response.text == "anthropic response"
        await router.aclose()

    async def test_respects_agent_preference_order(self, monkeypatch):
        # backend-engineer prefers openai before anthropic (§E7 example mapping).
        router = ModelRouter(_settings(anthropic_api_key="k1", openai_api_key="k2"))

        attempted: list[str] = []

        async def fake_invoke(provider, system_prompt, user_prompt):
            attempted.append(provider)
            return "ok", "model", 10

        monkeypatch.setattr(router, "_invoke", fake_invoke)

        await router.complete("backend-engineer", "system", "prompt")
        assert attempted[0] == "openai"
        await router.aclose()
