"""Runtime configuration, sourced entirely from environment variables.

Architecture rule (ARCHITECTURE.md §2 two-runtime split): this process never
opens a database connection of its own. `api_base_url` is the only path to
persistence — every fact this runtime needs (workflows, DAG state, registry,
memory, artifacts) is read or written through that HTTP API.
"""

from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="", case_sensitive=False)

    api_base_url: str = "http://localhost:5080"
    redis_url: str = "redis://localhost:6380"

    # Multi-Model Router (ARCHITECTURE_EXTENSION.md §E7) provider credentials.
    # Any combination may be absent — the router falls back to the next
    # configured provider, and finally to a deterministic mock so the whole
    # pipeline is runnable with zero credentials configured.
    anthropic_api_key: str | None = None
    openai_api_key: str | None = None
    gemini_api_key: str | None = None
    ollama_host: str | None = None

    heartbeat_interval_seconds: float = 15.0
    consumer_group: str = "ai-runtime-agents"
    consumer_name: str = "ai-runtime-1"

    log_level: str = "INFO"


@lru_cache
def get_settings() -> Settings:
    return Settings()
