from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FrontendEngineerAgent(AgentBase):
    name = "frontend-engineer"
    description = "Implements frontend features per the project's frontend conventions."
    skills = ["nextjs", "react", "frontend"]
    supported_tasks = ["ImplementFrontendFeature"]
    priority = 60
    required_context = ["ArchitectureDoc"]
    produced_artifacts = ["FrontendCode"]
    tools_available = ["prompt_loader", "artifact_store", "filesystem"]
    permissions = ["prompt_loader", "artifact_store", "filesystem"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "frontend_engineer", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="FrontendCode", artifact_type="Code", content=content)
