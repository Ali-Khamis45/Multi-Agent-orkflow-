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
        return await self.produce_artifact(ctx, name="FinancialProjection", artifact_type="Markdown", content=content)
