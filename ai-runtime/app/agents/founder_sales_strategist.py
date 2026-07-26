from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderSalesStrategistAgent(AgentBase):
    name = "founder-sales-strategist"
    company_type = "Founder"
    description = "Sales channel strategy and conversion approach."
    skills = ["sales-strategy", "channel-planning"]
    supported_tasks = ["DefineSalesStrategy"]
    priority = 70
    required_context = ["BrandIdentity"]
    produced_artifacts = ["SalesStrategy"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_sales_strategist", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        await self.update_company_profile(
            ctx, "products", content,
            {"pricingStrategy": "one short phrase describing the pricing/sales approach", "notes": "2-3 sentence sales channel summary"},
        )
        return await self.produce_artifact(ctx, name="SalesStrategy", artifact_type="Markdown", content=content)
