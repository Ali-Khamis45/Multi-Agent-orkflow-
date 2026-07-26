from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderCustomerResearcherAgent(AgentBase):
    name = "founder-customer-researcher"
    company_type = "Founder"
    description = "Target audience definition and customer personas."
    skills = ["customer-research", "persona-development"]
    supported_tasks = ["ResearchCustomers"]
    priority = 80
    required_context = ["BusinessModelCanvas"]
    produced_artifacts = ["CustomerPersonas"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_customer_researcher", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        await self.update_company_profile(
            ctx, "customers", content,
            {
                "personas": "array of objects {name, description} for each customer persona defined",
                "problems": "array of short strings, customer problems/pain points identified",
                "goals": "array of short strings, customer goals identified",
            },
        )
        return await self.produce_artifact(ctx, name="CustomerPersonas", artifact_type="Markdown", content=content)
