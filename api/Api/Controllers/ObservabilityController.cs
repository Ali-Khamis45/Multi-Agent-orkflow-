using AiAgentsTeam.Application.Observability.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Agent Metrics (Phase 1.5 §6) — the data foundation for Observability 2.0 (§E11).</summary>
[ApiController]
[Route("api/observability")]
public sealed class ObservabilityController(ISender sender) : ControllerBase
{
    [HttpGet("agents/metrics")]
    public async Task<ActionResult<IReadOnlyCollection<AgentMetricsDto>>> GetAgentMetrics(
        [FromQuery] string? agentName, CancellationToken ct) =>
        Ok(await sender.Send(new GetAgentMetricsQuery(agentName), ct));

    [HttpGet("agents/{agentName}/confidence-trend")]
    public async Task<ActionResult<IReadOnlyCollection<ConfidencePointDto>>> GetConfidenceTrend(
        string agentName, [FromQuery] int limit, CancellationToken ct) =>
        Ok(await sender.Send(new GetAgentConfidenceTrendQuery(agentName, limit == 0 ? 50 : limit), ct));
}
