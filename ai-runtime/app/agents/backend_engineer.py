from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class BackendEngineerAgent(AgentBase):
    name = "backend-engineer"
    description = "Implements backend features per Clean Architecture + CQRS conventions."
    skills = ["dotnet", "backend", "cqrs"]
    supported_tasks = ["ImplementBackendFeature"]
    priority = 60
    required_context = ["ArchitectureDoc"]
    produced_artifacts = ["BackendCode"]
    tools_available = ["prompt_loader", "artifact_store", "filesystem"]
    permissions = ["prompt_loader", "artifact_store", "filesystem"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "backend_engineer", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="BackendCode", artifact_type="Code", content=content)
