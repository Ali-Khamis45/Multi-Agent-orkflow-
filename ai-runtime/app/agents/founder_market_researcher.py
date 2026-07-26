from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderMarketResearcherAgent(AgentBase):
    name = "founder-market-researcher"
    company_type = "Founder"
    description = "Market sizing and competitor analysis."
    skills = ["market-research", "competitor-analysis"]
    supported_tasks = ["ResearchMarket"]
    priority = 80
    required_context = ["BusinessModelCanvas"]
    produced_artifacts = ["MarketResearchReport"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_market_researcher", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        await self.update_company_profile(
            ctx, "competition", content,
            {
                "competitors": "array of objects {name, strengths, weaknesses} for each competitor mentioned",
                "opportunities": "array of short strings, market opportunities identified",
                "notes": "2-3 sentence market-size/positioning summary",
            },
        )
        await self.update_company_profile(
            ctx, "customers", content,
            {"targetAudience": "one or two sentence description of the target audience implied by this market analysis"},
        )
        return await self.produce_artifact(ctx, name="MarketResearchReport", artifact_type="Markdown", content=content)
