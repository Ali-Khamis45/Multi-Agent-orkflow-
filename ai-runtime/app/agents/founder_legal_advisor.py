from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderLegalAdvisorAgent(AgentBase):
    name = "founder-legal-advisor"
    company_type = "Founder"
    description = "Final compliance and legal-risk pass on the whole venture plan before launch."
    skills = ["legal-review", "compliance"]
    supported_tasks = ["ReviewLegalRisk"]
    priority = 60
    required_context = ["GrowthRoadmap"]
    produced_artifacts = ["LaunchStrategy"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_legal_advisor", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="LaunchStrategy", artifact_type="Markdown", content=content)
