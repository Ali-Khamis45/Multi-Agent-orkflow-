from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderBrandStrategistAgent(AgentBase):
    name = "founder-brand-strategist"
    company_type = "Founder"
    description = "Brand identity, mission, and vision, informed by market and customer research."
    skills = ["brand-strategy", "positioning"]
    supported_tasks = ["DefineBrandIdentity"]
    priority = 75
    required_context = ["MarketResearchReport", "CustomerPersonas"]
    produced_artifacts = ["BrandIdentity"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_brand_strategist", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        await self.update_company_profile(
            ctx, "brand", content,
            {
                "mission": "one sentence mission statement",
                "vision": "one sentence vision statement",
                "brandPersonality": "a few adjectives describing the brand personality",
                "brandVoice": "one short phrase describing the brand's tone of voice",
                "slogan": "a short memorable slogan/tagline, or null if none was proposed",
            },
        )
        return await self.produce_artifact(ctx, name="BrandIdentity", artifact_type="Markdown", content=content)
