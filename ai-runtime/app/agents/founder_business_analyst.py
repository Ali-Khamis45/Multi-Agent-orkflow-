from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderBusinessAnalystAgent(AgentBase):
    name = "founder-business-analyst"
    company_type = "Founder"
    description = "Business Model Canvas and SWOT analysis for the venture."
    skills = ["business-modeling", "swot-analysis"]
    supported_tasks = ["AnalyzeBusinessModel"]
    priority = 85
    required_context = ["ExecutiveSummary"]
    produced_artifacts = ["BusinessModelCanvas"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_business_analyst", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="BusinessModelCanvas", artifact_type="Markdown", content=content)
