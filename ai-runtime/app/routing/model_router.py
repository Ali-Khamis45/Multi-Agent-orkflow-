"""Multi-Model Router (ARCHITECTURE_EXTENSION.md §E7). Chooses a model per
agent from a preference order, with automatic fallback across providers —
concretely implementing the example mapping the spec gives (Architect→Claude,
Backend→GPT, Reviewer→Claude, Documentation→Gemini, cheap tasks→Ollama).

If none of a workspace's configured providers are reachable (no API key, or
sandbox with no network egress to LLM providers), the router falls back to a
deterministic mock completion — clearly labeled as such in its output — so
the whole reasoning pipeline and DAG remain exercisable end-to-end without
live credentials. Swapping in real credentials changes nothing else in the
system; this is the only place a provider distinction is made.
"""

from __future__ import annotations

from dataclasses import dataclass

import httpx

from app.config import Settings
from app.logging_config import get_logger

logger = get_logger(__name__)

# §E7's example mapping, in preference order per agent capability.
AGENT_MODEL_PREFERENCE: dict[str, list[str]] = {
    "system-architect": ["anthropic", "openai", "gemini", "ollama"],
    "backend-engineer": ["openai", "anthropic", "gemini", "ollama"],
    "frontend-engineer": ["openai", "anthropic", "gemini", "ollama"],
    "code-reviewer": ["anthropic", "openai", "gemini", "ollama"],
    "qa-engineer": ["anthropic", "openai", "gemini", "ollama"],
    "business-analyst": ["anthropic", "openai", "gemini", "ollama"],
    "project-manager": ["gemini", "anthropic", "openai", "ollama"],
    "intent-engine": ["anthropic", "openai", "gemini", "ollama"],
    "supervisor": ["anthropic", "openai", "gemini", "ollama"],
}
DEFAULT_PREFERENCE = ["anthropic", "openai", "gemini", "ollama"]

# Cost Efficiency placeholder (ARCHITECTURE_EXTENSION.md §E7/§E15) — rough,
# illustrative $-per-1K-tokens rates, not each provider's actual current
# pricing. Good enough to produce a non-zero, comparable CostEstimate signal
# per stage until a real pricing table/billing integration exists.
_PLACEHOLDER_PRICE_PER_1K_TOKENS: dict[str, float] = {
    "anthropic": 0.015,
    "openai": 0.01,
    "gemini": 0.007,
    "ollama": 0.0,
    "mock": 0.0,
}


@dataclass
class ModelResponse:
    text: str
    provider: str
    model: str
    fallback_triggered: bool
    tokens: int
    cost_estimate: float


class ModelRouter:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._http = httpx.AsyncClient(timeout=60.0)

    async def aclose(self) -> None:
        await self._http.aclose()

    def _is_configured(self, provider: str) -> bool:
        return {
            "anthropic": bool(self._settings.anthropic_api_key),
            "openai": bool(self._settings.openai_api_key),
            "gemini": bool(self._settings.gemini_api_key),
            "ollama": bool(self._settings.ollama_host),
        }.get(provider, False)

    async def complete(self, agent_capability: str, system_prompt: str, user_prompt: str) -> ModelResponse:
        preference = AGENT_MODEL_PREFERENCE.get(agent_capability, DEFAULT_PREFERENCE)
        configured = [p for p in preference if self._is_configured(p)]

        fallback_triggered = False
        for provider in configured:
            try:
                text, model, tokens = await self._invoke(provider, system_prompt, user_prompt)
                cost = round(tokens / 1000 * _PLACEHOLDER_PRICE_PER_1K_TOKENS.get(provider, 0.0), 6)
                return ModelResponse(
                    text=text, provider=provider, model=model,
                    fallback_triggered=fallback_triggered, tokens=tokens, cost_estimate=cost,
                )
            except Exception:
                logger.exception("model provider failed; trying next", extra={"fields": {"provider": provider}})
                fallback_triggered = True
                continue

        # No provider configured/reachable — deterministic mock keeps the
        # pipeline runnable end-to-end (this sandbox has no LLM credentials).
        text = self._mock_completion(agent_capability, user_prompt)
        return ModelResponse(
            text=text,
            provider="mock",
            model="mock-deterministic",
            fallback_triggered=len(configured) > 0,
            tokens=self._estimate_tokens(text),
            cost_estimate=0.0,
        )

    async def _invoke(self, provider: str, system_prompt: str, user_prompt: str) -> tuple[str, str, int]:
        if provider == "anthropic":
            return await self._invoke_anthropic(system_prompt, user_prompt)
        if provider == "openai":
            return await self._invoke_openai(system_prompt, user_prompt)
        if provider == "gemini":
            return await self._invoke_gemini(system_prompt, user_prompt)
        if provider == "ollama":
            return await self._invoke_ollama(system_prompt, user_prompt)
        raise ValueError(f"Unknown provider '{provider}'")

    @staticmethod
    def _estimate_tokens(text: str) -> int:
        """~4 characters/token is a common rough estimate — used only when a
        provider response doesn't report real usage (Ollama today)."""
        return max(1, len(text) // 4)

    async def _invoke_anthropic(self, system_prompt: str, user_prompt: str) -> tuple[str, str, int]:
        model = "claude-sonnet-5"
        r = await self._http.post(
            "https://api.anthropic.com/v1/messages",
            headers={
                "x-api-key": self._settings.anthropic_api_key or "",
                "anthropic-version": "2023-06-01",
                "content-type": "application/json",
            },
            json={
                "model": model,
                "max_tokens": 2048,
                "system": system_prompt,
                "messages": [{"role": "user", "content": user_prompt}],
            },
        )
        r.raise_for_status()
        data = r.json()
        text = "".join(block.get("text", "") for block in data.get("content", []))
        usage = data.get("usage", {})
        tokens = usage.get("input_tokens", 0) + usage.get("output_tokens", 0) or self._estimate_tokens(text)
        return text, model, tokens

    async def _invoke_openai(self, system_prompt: str, user_prompt: str) -> tuple[str, str, int]:
        model = "gpt-5"
        r = await self._http.post(
            "https://api.openai.com/v1/chat/completions",
            headers={"Authorization": f"Bearer {self._settings.openai_api_key}"},
            json={
                "model": model,
                "messages": [
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
            },
        )
        r.raise_for_status()
        data = r.json()
        text = data["choices"][0]["message"]["content"]
        tokens = data.get("usage", {}).get("total_tokens") or self._estimate_tokens(text)
        return text, model, tokens

    async def _invoke_gemini(self, system_prompt: str, user_prompt: str) -> tuple[str, str, int]:
        model = "gemini-2.5-pro"
        r = await self._http.post(
            f"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent",
            params={"key": self._settings.gemini_api_key},
            json={
                "systemInstruction": {"parts": [{"text": system_prompt}]},
                "contents": [{"parts": [{"text": user_prompt}]}],
            },
        )
        r.raise_for_status()
        data = r.json()
        text = "".join(part.get("text", "") for part in data["candidates"][0]["content"]["parts"])
        tokens = data.get("usageMetadata", {}).get("totalTokenCount") or self._estimate_tokens(text)
        return text, model, tokens

    async def _invoke_ollama(self, system_prompt: str, user_prompt: str) -> tuple[str, str, int]:
        model = "llama3"
        r = await self._http.post(
            f"{self._settings.ollama_host}/api/generate",
            json={"model": model, "prompt": f"{system_prompt}\n\n{user_prompt}", "stream": False},
        )
        r.raise_for_status()
        data = r.json()
        text = data["response"]
        tokens = (data.get("prompt_eval_count", 0) + data.get("eval_count", 0)) or self._estimate_tokens(text)
        return text, model, tokens

    @staticmethod
    def _mock_completion(agent_capability: str, user_prompt: str) -> str:
        return (
            f"[MOCK completion — no LLM provider configured]\n"
            f"Agent: {agent_capability}\n"
            f"Deterministic placeholder output produced for prompt of {len(user_prompt)} chars. "
            f"Configure ANTHROPIC_API_KEY / OPENAI_API_KEY / GEMINI_API_KEY / OLLAMA_HOST "
            f"to replace this with a real model response."
        )
