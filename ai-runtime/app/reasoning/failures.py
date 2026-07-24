"""Structured Error Model (Phase 1.5 §4) — every task failure this runtime
publishes has this fixed shape (mirrors AiAgentsTeam.Domain.Failures.
ExecutionFailure), not a bare exception string. FailTaskCommandHandler on the
.NET side parses this exact JSON shape into a durable, queryable record.
"""

from __future__ import annotations

import traceback
from dataclasses import asdict, dataclass

import httpx

from app.tools.base import ToolError


class FailureCategory:
    VALIDATION = "Validation"
    TRANSIENT = "Transient"
    PERMISSION = "Permission"
    PROVIDER = "Provider"
    TIMEOUT = "Timeout"
    UNKNOWN = "Unknown"


class FailureSeverity:
    LOW = "Low"
    MEDIUM = "Medium"
    HIGH = "High"
    CRITICAL = "Critical"


@dataclass
class StructuredFailure:
    category: str
    severity: str
    recoverable: bool
    retryable: bool
    message: str
    stack: str | None
    suggested_action: str | None

    def to_reason_json_dict(self) -> dict:
        """camelCase keys — this ReasonJson payload is parsed by .NET's
        FailTaskCommandHandler.ParseStructuredFailure, which (since Python, not
        System.Text.Json, authors this particular sub-payload) expects camelCase
        property names, not Python's snake_case field names."""
        data = asdict(self)
        data["suggestedAction"] = data.pop("suggested_action")
        return data


def classify_exception(exc: BaseException) -> StructuredFailure:
    """Best-effort classification — good enough to drive retry/severity
    decisions without every agent hand-writing its own try/except mapping."""
    stack = "".join(traceback.format_exception(type(exc), exc, exc.__traceback__))[-4000:]

    if isinstance(exc, ToolError):
        return StructuredFailure(
            category=FailureCategory.VALIDATION,
            severity=FailureSeverity.LOW,
            recoverable=True,
            retryable=False,
            message=str(exc),
            stack=stack,
            suggested_action="Check the tool call's parameters against its declared schema.",
        )

    if isinstance(exc, httpx.TimeoutException):
        return StructuredFailure(
            category=FailureCategory.TIMEOUT,
            severity=FailureSeverity.MEDIUM,
            recoverable=True,
            retryable=True,
            message=str(exc) or "Request timed out.",
            stack=stack,
            suggested_action="Retry; if it persists, check the downstream service's health.",
        )

    if isinstance(exc, httpx.HTTPStatusError):
        status = exc.response.status_code
        if status == 401 or status == 403:
            return StructuredFailure(
                category=FailureCategory.PERMISSION,
                severity=FailureSeverity.HIGH,
                recoverable=False,
                retryable=False,
                message=f"HTTP {status}: {exc}",
                stack=stack,
                suggested_action="Check API credentials/permissions — retrying will not help.",
            )
        if status == 429 or status >= 500:
            return StructuredFailure(
                category=FailureCategory.PROVIDER,
                severity=FailureSeverity.MEDIUM,
                recoverable=True,
                retryable=True,
                message=f"HTTP {status}: {exc}",
                stack=stack,
                suggested_action="Transient provider/API error — safe to retry.",
            )
        return StructuredFailure(
            category=FailureCategory.VALIDATION,
            severity=FailureSeverity.MEDIUM,
            recoverable=True,
            retryable=False,
            message=f"HTTP {status}: {exc}",
            stack=stack,
            suggested_action="Check the request payload sent to the API.",
        )

    if isinstance(exc, httpx.ConnectError):
        return StructuredFailure(
            category=FailureCategory.TRANSIENT,
            severity=FailureSeverity.MEDIUM,
            recoverable=True,
            retryable=True,
            message=str(exc),
            stack=stack,
            suggested_action="Downstream service unreachable — safe to retry once it's back.",
        )

    return StructuredFailure(
        category=FailureCategory.UNKNOWN,
        severity=FailureSeverity.MEDIUM,
        recoverable=False,
        retryable=True,
        message=str(exc) or exc.__class__.__name__,
        stack=stack,
        suggested_action=None,
    )
