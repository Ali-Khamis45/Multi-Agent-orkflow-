from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class QaEngineerAgent(AgentBase):
    name = "qa-engineer"
    description = "Test planning, test cases, pass/fail quality assessment."
    skills = ["testing", "qa"]
    supported_tasks = ["RunQA"]
    priority = 50
    required_context = ["CodeReviewReport"]
    produced_artifacts = ["QAReport"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "qa_engineer", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="QAReport", artifact_type="Markdown", content=content)
