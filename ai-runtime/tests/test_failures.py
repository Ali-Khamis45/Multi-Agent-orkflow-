"""Structured Error Model (Phase 1.5 §4) — exception classification and the
camelCase wire shape .NET's FailTaskCommandHandler expects."""

import httpx

from app.reasoning.failures import FailureCategory, FailureSeverity, ToolError, classify_exception


def _http_error(status: int) -> httpx.HTTPStatusError:
    request = httpx.Request("GET", "http://example.test")
    response = httpx.Response(status, request=request)
    try:
        response.raise_for_status()
    except httpx.HTTPStatusError as exc:
        return exc
    raise AssertionError("expected raise_for_status to raise")


class TestClassifyException:
    def test_tool_error_is_validation_and_not_retryable(self):
        failure = classify_exception(ToolError("bad params"))
        assert failure.category == FailureCategory.VALIDATION
        assert failure.recoverable is True
        assert failure.retryable is False

    def test_permission_denied_is_not_retryable(self):
        failure = classify_exception(_http_error(403))
        assert failure.category == FailureCategory.PERMISSION
        assert failure.severity == FailureSeverity.HIGH
        assert failure.retryable is False

    def test_rate_limit_is_provider_and_retryable(self):
        failure = classify_exception(_http_error(429))
        assert failure.category == FailureCategory.PROVIDER
        assert failure.retryable is True

    def test_server_error_is_provider_and_retryable(self):
        failure = classify_exception(_http_error(500))
        assert failure.category == FailureCategory.PROVIDER
        assert failure.retryable is True

    def test_bad_request_is_validation_and_not_retryable(self):
        failure = classify_exception(_http_error(400))
        assert failure.category == FailureCategory.VALIDATION
        assert failure.retryable is False

    def test_connect_error_is_transient_and_retryable(self):
        request = httpx.Request("GET", "http://example.test")
        failure = classify_exception(httpx.ConnectError("refused", request=request))
        assert failure.category == FailureCategory.TRANSIENT
        assert failure.retryable is True

    def test_unknown_exception_defaults_to_retryable_unknown(self):
        failure = classify_exception(ValueError("something odd"))
        assert failure.category == FailureCategory.UNKNOWN
        assert failure.retryable is True

    def test_wire_shape_uses_camel_case_suggested_action(self):
        failure = classify_exception(_http_error(403))
        payload = failure.to_reason_json_dict()
        assert "suggestedAction" in payload
        assert "suggested_action" not in payload
        assert payload["category"] == "Permission"
