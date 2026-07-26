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
    tools_available = ["prompt_loader", "artifact_store", "connector_action"]
    permissions = ["prompt_loader", "artifact_store", "connector_action"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "code_reviewer", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )

        # Phase 4 "Connector Framework": "... modify code -> commit -> Pull Request."
        # Commits the review report itself (the artifact this agent actually owns) —
        # best-effort, most workspaces won't have GitHub connected.
        await self.try_connector_action(
            ctx, "github", "CommitFile",
            {
                "owner": ctx.inputs.get("repoOwner", "workspace"),
                "repo": ctx.inputs.get("repoName", "project"),
                "path": f"reports/code-review-{ctx.task_node_id}.md",
                "branch": ctx.inputs.get("branch", "main"),
                "message": f"Add code review report: {ctx.inputs.get('goal', '')[:72]}",
                "content": content,
            },
        )

        return await self.produce_artifact(ctx, name="CodeReviewReport", artifact_type="Markdown", content=content)
