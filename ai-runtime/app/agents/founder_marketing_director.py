from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderMarketingDirectorAgent(AgentBase):
    name = "founder-marketing-director"
    company_type = "Founder"
    description = "Pricing strategy and a go-to-market marketing plan."
    skills = ["marketing-strategy", "pricing"]
    supported_tasks = ["CreateMarketingPlan"]
    priority = 70
    required_context = ["BrandIdentity"]
    produced_artifacts = ["MarketingPlan"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_marketing_director", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        await self.update_company_profile(
            ctx, "marketing", content,
            {
                "channels": "array of short strings, marketing channels recommended",
                "contentStyle": "one short phrase describing the content style/tone",
                "socialPlatforms": "array of short strings, social platforms recommended",
                "notes": "2-3 sentence go-to-market summary",
            },
        )
        return await self.produce_artifact(ctx, name="MarketingPlan", artifact_type="Markdown", content=content)
