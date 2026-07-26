from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderFinancialAdvisorAgent(AgentBase):
    name = "founder-financial-advisor"
    company_type = "Founder"
    description = "Financial projections and a funding strategy."
    skills = ["financial-modeling", "fundraising"]
    supported_tasks = ["ProjectFinancials"]
    priority = 70
    required_context = ["BrandIdentity"]
    produced_artifacts = ["FinancialProjection"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_financial_advisor", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        await self.update_company_profile(
            ctx, "business", content,
            {
                "budget": "estimated starting budget as a plain number (no currency symbol), or null if not estimated",
                "monthlyRevenueGoal": "estimated monthly revenue goal as a plain number, or null if not estimated",
                "fundingStatus": "one short phrase describing funding status, e.g. Bootstrapped, Seed-stage, Seeking investment",
                "notes": "2-3 sentence summary of cash flow and pricing considerations",
            },
        )
        return await self.produce_artifact(ctx, name="FinancialProjection", artifact_type="Markdown", content=content)
