using AiAgentsTeam.Application.Reasoning.Commands;
using AiAgentsTeam.Application.Reasoning.Queries;
using AiAgentsTeam.Domain.Reasoning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Reasoning Engine trace endpoints (ARCHITECTURE_EXTENSION.md §E6, Phase 1.5 §1) — every agent's 12-stage pipeline.</summary>
[ApiController]
[Route("api/reasoning")]
public sealed class ReasoningController(ISender sender) : ControllerBase
{
    public sealed record RecordTraceRequest(
        Guid TaskNodeId,
        string Agent,
        ReasoningStage Stage,
        DateTimeOffset StartedAt,
        long DurationMs,
        string? InputJson,
        string? OutputJson,
        int? Tokens,
        double? Confidence,
        string? ModelUsed,
        int RetryCount,
        int MemoryReads,
        int MemoryWrites,
        int ToolCalls,
        double? CostEstimate,
        string? ErrorMessage);

    [HttpPost("traces")]
    public async Task<ActionResult<Guid>> RecordTrace(RecordTraceRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new RecordReasoningTraceCommand(
            request.TaskNodeId, request.Agent, request.Stage, request.StartedAt, request.DurationMs,
            request.InputJson, request.OutputJson, request.Tokens, request.Confidence, request.ModelUsed,
            request.RetryCount, request.MemoryReads, request.MemoryWrites, request.ToolCalls,
            request.CostEstimate, request.ErrorMessage), ct);
        return Ok(id);
    }

    [HttpGet("traces/{taskNodeId:guid}")]
    public async Task<ActionResult<IReadOnlyCollection<ReasoningTraceDto>>> GetTraces(Guid taskNodeId, CancellationToken ct) =>
        Ok(await sender.Send(new GetReasoningTracesQuery(taskNodeId), ct));

    [HttpGet("telemetry")]
    public async Task<ActionResult<ReasoningTelemetryDto>> GetTelemetry(
        [FromQuery] Guid workspaceId, [FromQuery] int pointsLimit, CancellationToken ct) =>
        Ok(await sender.Send(new GetReasoningTelemetryQuery(workspaceId, pointsLimit == 0 ? 300 : pointsLimit), ct));

    [HttpGet("agents/{agentName}/traces")]
    public async Task<ActionResult<IReadOnlyCollection<ReasoningTraceDto>>> GetAgentTraces(
        string agentName, [FromQuery] int limit, CancellationToken ct) =>
        Ok(await sender.Send(new GetAgentReasoningTracesQuery(agentName, limit == 0 ? 100 : limit), ct));
}
