from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderGrowthStrategistAgent(AgentBase):
    name = "founder-growth-strategist"
    company_type = "Founder"
    description = "Growth roadmap and a 90-day execution plan, synthesizing finance/marketing/ops/sales."
    skills = ["growth-strategy", "execution-planning"]
    supported_tasks = ["PlanGrowth"]
    priority = 65
    required_context = ["FinancialProjection", "MarketingPlan", "OperationsPlan", "SalesStrategy"]
    produced_artifacts = ["GrowthRoadmap"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_growth_strategist", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="GrowthRoadmap", artifact_type="Markdown", content=content)
