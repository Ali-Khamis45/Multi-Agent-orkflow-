"""Runtime configuration, sourced entirely from environment variables — no
value an operator might need to change between Development/Testing/
Production/Docker/Local/Cloud is hardcoded in application code (Phase 1.5
Configuration Layer hardening). See .env.example for the full variable list.

Architecture rule (ARCHITECTURE.md §2 two-runtime split): this process never
opens a database connection of its own. `api_base_url` is the only path to
persistence — every fact this runtime needs (workflows, DAG state, registry,
memory, artifacts) is read or written through that HTTP API.
"""

from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="", case_sensitive=False)

    environment: str = "Local"  # Local | Development | Testing | Docker | Production | Cloud

    api_base_url: str = "http://localhost:5080"
    redis_url: str = "redis://localhost:6380"

    # How other processes (the .NET Scheduler, a future distributed cluster,
    # §E16) reach *this* runtime's agents — was hardcoded as
    # "http://ai-runtime:8000" in Milestone 2; now an operator can point it at
    # whatever hostname/port this container is actually reachable on.
    self_base_url: str = "http://localhost:8000"

    # Filesystem tool sandbox root (§8 Tool Marketplace / Phase 1.5 §7 Tool
    # Sandbox) — was hardcoded to "/data/workspace-files".
    workspace_files_root: str = "./.data/workspace-files"

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

    # Tool sandbox hardening (Phase 1.5 §7).
    filesystem_max_bytes: int = 1_000_000

    # Phase 2 auth: this process authenticates its own service-to-service calls
    # (e.g. bootstrapping its legacy default Workspace at startup) with a shared
    # key rather than a user JWT, since it has no user session. Local-dev-only
    # default, same treatment as the API's Jwt:Secret — must be overridden
    # together with the API's Internal:ServiceKey in any real deployment.
    internal_service_key: str = "local-dev-only-secret-change-me-before-any-real-deployment-32chars"

    log_level: str = "INFO"


@lru_cache
def get_settings() -> Settings:
    return Settings()
