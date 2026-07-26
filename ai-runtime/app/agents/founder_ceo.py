from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderCeoAgent(AgentBase):
    name = "founder-ceo"
    company_type = "Founder"
    description = "Sets the overall business vision and frames the executive summary for the venture."
    skills = ["business-strategy", "executive-communication"]
    supported_tasks = ["FrameVenture"]
    priority = 90
    produced_artifacts = ["ExecutiveSummary"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_ceo", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        await self.update_company_profile(
            ctx, "business", content,
            {"growthGoal": "one sentence overall growth ambition for the business", "notes": "2-3 sentence synthesis of the business vision and direction"},
        )
        return await self.produce_artifact(ctx, name="ExecutiveSummary", artifact_type="Markdown", content=content)
