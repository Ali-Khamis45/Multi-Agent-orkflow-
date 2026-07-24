"""Intent Engine (ARCHITECTURE_EXTENSION.md §E2) — deliberately NOT an AgentBase
subclass. It is not one of the seven Phase 1 agents; it is the pre-DAG intake
phase that runs before any TaskNode exists, producing the StructuredRequirements
artifact the Business Analyst's DAG node consumes as upstream context.

Phase 1 scope: goal extraction, project classification, a heuristic complexity
score, and ambiguity detection. If ambiguity is found, Phase 1 does not block on
a human clarification round (no frontend exists yet to answer one, and Approval
Gates are out of Phase 1's mandatory scope per ARCHITECTURE.md §24) — it
proceeds with the ambiguity documented as an assumption in the structured
output, matching the precedent ARCHITECTURE.md §25 already sets for this
exact situation.
"""

from __future__ import annotations

import json
import uuid

from app.clients.api_client import ApiClient
from app.logging_config import get_logger
from app.routing.model_router import ModelRouter
from app.tools.registry import ToolRegistry

logger = get_logger(__name__)

PROJECT_TYPES = ["CRUD", "SaaS", "AI System", "Microservices", "Mobile App", "Game", "Enterprise", "Dashboard"]


class IntentEngine:
    def __init__(self, api: ApiClient, model_router: ModelRouter, tools: ToolRegistry) -> None:
        self._api = api
        self._router = model_router
        self._tools = tools

    async def run(self, workspace_id: uuid.UUID, workflow_run_id: uuid.UUID, raw_input: str) -> uuid.UUID:
        session_id = await self._api.start_intent_session(workspace_id, raw_input)

        rendered = await self._tools.invoke(
            "prompt_loader", [], {"template": "intent_engine", "variables": {"raw_input": raw_input}}
        )
        if not rendered.success:
            raise RuntimeError(f"Intent Engine prompt render failed: {rendered.error}")

        response = await self._router.complete(
            "intent-engine",
            system_prompt="You are the Intent Engine of an autonomous AI software engineering company.",
            user_prompt=rendered.output,
        )

        classification = self._classify(response.text)
        has_ambiguity = "no ambiguity" not in response.text.lower()
        complexity_score = self._estimate_complexity(raw_input, response.text)

        await self._api.record_intent_analysis(
            session_id=session_id,
            extracted_goals_json=json.dumps({"analysis": response.text[:2000]}),
            project_classification=classification,
            complexity_score=complexity_score,
            risk_flags_json=json.dumps(self._extract_risk_lines(response.text)),
            ambiguities_json=json.dumps({"analysis": response.text[:2000]}),
            has_ambiguity=has_ambiguity,
        )

        structured_content = self._render_structured_requirements(
            raw_input, response.text, classification, complexity_score, has_ambiguity
        )
        artifact_id = await self._api.create_artifact(
            workspace_id=workspace_id,
            name="StructuredRequirements",
            artifact_type="Markdown",
            owner_agent="intent-engine",
            content=structured_content,
            workflow_run_id=workflow_run_id,
        )
        await self._api.mark_intent_structured(session_id, artifact_id)

        logger.info(
            "intent analysis complete",
            extra={"fields": {"classification": classification, "complexity": complexity_score, "ambiguous": has_ambiguity}},
        )
        return artifact_id

    @staticmethod
    def _classify(analysis_text: str) -> str:
        lowered = analysis_text.lower()
        for project_type in PROJECT_TYPES:
            if project_type.lower() in lowered:
                return project_type
        return "SaaS"

    @staticmethod
    def _estimate_complexity(raw_input: str, analysis_text: str) -> float:
        # Heuristic placeholder (no structured-output guarantee across providers/mock):
        # longer requests with more distinct requirement-shaped sentences score higher.
        signal = len(raw_input.split()) + analysis_text.lower().count("integrat") * 5
        return round(min(1.0, max(0.1, signal / 120)), 2)

    @staticmethod
    def _extract_risk_lines(analysis_text: str) -> list[str]:
        return [line.strip() for line in analysis_text.splitlines() if "risk" in line.lower()]

    @staticmethod
    def _render_structured_requirements(
        raw_input: str, analysis_text: str, classification: str, complexity_score: float, has_ambiguity: bool
    ) -> str:
        assumptions_note = (
            "\n\n## Assumptions\nAmbiguity was detected but Phase 1 proceeds without a human "
            "clarification round (no approval gate configured yet) — downstream agents should "
            "treat this analysis as best-effort.\n"
            if has_ambiguity
            else ""
        )
        return (
            f"# Structured Requirements\n\n"
            f"**Project classification:** {classification}\n"
            f"**Complexity score:** {complexity_score}\n\n"
            f"## Raw request\n{raw_input}\n\n"
            f"## Intent analysis\n{analysis_text}"
            f"{assumptions_note}"
        )
