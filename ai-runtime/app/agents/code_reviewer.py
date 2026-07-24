from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class CodeReviewerAgent(AgentBase):
    name = "code-reviewer"
    description = "Code review: correctness, architecture conformance, approve/reject verdict."
    skills = ["code-review", "static-analysis"]
    supported_tasks = ["ReviewCode"]
    priority = 55
    required_context = ["BackendCode", "FrontendCode"]
    produced_artifacts = ["CodeReviewReport"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "code_reviewer", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="CodeReviewReport", artifact_type="Markdown", content=content)
