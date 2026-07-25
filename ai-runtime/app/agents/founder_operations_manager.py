from app.agents.base import AgentBase
from app.reasoning.pipeline import AgentContext, DomainResult


class FounderOperationsManagerAgent(AgentBase):
    name = "founder-operations-manager"
    company_type = "Founder"
    description = "Manufacturing/fulfillment planning and operational risk assessment."
    skills = ["operations-planning", "risk-assessment"]
    supported_tasks = ["PlanOperations"]
    priority = 70
    required_context = ["BrandIdentity"]
    produced_artifacts = ["OperationsPlan"]
    tools_available = ["prompt_loader", "artifact_store"]
    permissions = ["prompt_loader", "artifact_store"]

    async def execute_domain_logic(self, ctx: AgentContext) -> DomainResult:
        content = await self.generate(
            ctx, "founder_operations_manager", goal=ctx.inputs.get("goal", ""), context=ctx.context_data or "(none provided)"
        )
        return await self.produce_artifact(ctx, name="OperationsPlan", artifact_type="Markdown", content=content)
