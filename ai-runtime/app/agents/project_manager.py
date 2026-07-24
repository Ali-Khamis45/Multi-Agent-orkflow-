from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class ProjectManagerAgent(AgentBase):
    name = "project-manager"
    description = "Prioritization, backlog, sprint planning, business value."
    skills = ["planning", "prioritization"]
    supported_tasks = ["PlanProject"]
    priority = 75
    required_context = ["UserStories"]
    produced_artifacts = ["TaskPlan"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "project_manager", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="TaskPlan", artifact_type="Markdown", content=content)
