from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class SystemArchitectAgent(AgentBase):
    name = "system-architect"
    description = "Architecture design: components, data model, API surfaces, tech choices."
    skills = ["architecture", "clean-architecture", "ddd", "cqrs"]
    supported_tasks = ["DesignArchitecture"]
    priority = 70
    required_context = ["TaskPlan"]
    produced_artifacts = ["ArchitectureDoc"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "system_architect", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="ArchitectureDoc", artifact_type="Markdown", content=content)
