from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class BusinessAnalystAgent(AgentBase):
    name = "business-analyst"
    description = "Requirement discovery, gap analysis, user stories, acceptance criteria."
    skills = ["requirements-analysis", "user-stories"]
    supported_tasks = ["DiscoverRequirements"]
    priority = 80
    required_context = ["StructuredRequirements"]
    produced_artifacts = ["UserStories"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "business_analyst", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="UserStories", artifact_type="Markdown", content=content)
