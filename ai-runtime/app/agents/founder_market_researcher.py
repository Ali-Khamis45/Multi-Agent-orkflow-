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
        return await self.produce_artifact(ctx, name="MarketResearchReport", artifact_type="Markdown", content=content)
