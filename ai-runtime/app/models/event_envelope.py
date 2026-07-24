"""Mirrors AiAgentsTeam.Application.Common.Messaging.EventEnvelope exactly
(ARCHITECTURE.md §6.2) — the one shared contract between the two runtimes.
The .NET side serializes with default System.Text.Json (PascalCase, no naming
policy), so every alias below must match the C# property name verbatim.
"""

from __future__ import annotations

import uuid
from datetime import datetime, timezone

from pydantic import BaseModel, ConfigDict, Field


class EventEnvelope(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    event_id: uuid.UUID = Field(default_factory=uuid.uuid4, alias="EventId")
    type: str = Field(alias="Type")
    version: int = Field(default=1, alias="Version")
    workspace_id: uuid.UUID = Field(alias="WorkspaceId")
    workflow_run_id: uuid.UUID | None = Field(default=None, alias="WorkflowRunId")
    task_id: uuid.UUID | None = Field(default=None, alias="TaskId")
    produced_by: str = Field(alias="ProducedBy")
    timestamp: datetime = Field(default_factory=lambda: datetime.now(timezone.utc), alias="Timestamp")
    payload_json: str = Field(alias="PayloadJson")
    confidence: float | None = Field(default=None, alias="Confidence")
    risk_level: str | None = Field(default=None, alias="RiskLevel")


class EventTypes:
    """Mirrors AiAgentsTeam.Application.Common.Messaging.EventTypes (§6.3)."""

    TASK_CREATED = "TaskCreated"
    TASK_DISPATCHED = "TaskDispatched"
    TASK_COMPLETED = "TaskCompleted"
    TASK_FAILED = "TaskFailed"
    WORKFLOW_RUN_STARTED = "WorkflowRunStarted"
    WORKFLOW_RUN_COMPLETED = "WorkflowRunCompleted"
    WORKFLOW_RUN_FAILED = "WorkflowRunFailed"
    AGENT_REGISTERED = "AgentRegistered"
    AGENT_UNAVAILABLE = "AgentUnavailable"

    SUPERVISOR_DIRECTIVE_ISSUED = "SupervisorDirectiveIssued"
    SUPERVISOR_REPLAN_REQUESTED = "SupervisorReplanRequested"
    SUPERVISOR_STRATEGY_SELECTED = "SupervisorStrategySelected"

    INTENT_ANALYSIS_STARTED = "IntentAnalysisStarted"
    CLARIFICATION_REQUESTED = "ClarificationRequested"
    CLARIFICATION_ANSWERED = "ClarificationAnswered"
    REQUIREMENTS_STRUCTURED = "RequirementsStructured"
    PROJECT_CLASSIFIED = "ProjectClassified"

    REASONING_STAGE_COMPLETED = "ReasoningStageCompleted"
