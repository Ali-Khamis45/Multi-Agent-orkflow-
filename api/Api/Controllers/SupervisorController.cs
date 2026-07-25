using AiAgentsTeam.Application.Supervisor.Commands;
using AiAgentsTeam.Application.Supervisor.Queries;
using AiAgentsTeam.Domain.Supervisor;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Supervisor Brain audit trail endpoint (ARCHITECTURE_EXTENSION.md §E1).</summary>
[ApiController]
[Route("api/supervisor")]
public sealed class SupervisorController(ISender sender) : ControllerBase
{
    public sealed record RecordDecisionRequest(
        Guid WorkflowRunId, SupervisorDecisionType DecisionType, string InputSnapshotJson,
        string Rationale, double Confidence, string? TargetNodeIdsJson);

    [HttpPost("decisions")]
    public async Task<ActionResult<Guid>> RecordDecision(RecordDecisionRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new RecordSupervisorDecisionCommand(
            request.WorkflowRunId, request.DecisionType, request.InputSnapshotJson,
            request.Rationale, request.Confidence, request.TargetNodeIdsJson), ct);
        return Ok(id);
    }

    [HttpGet("decisions")]
    public async Task<ActionResult<IReadOnlyCollection<SupervisorDecisionDto>>> GetDecisions(
        [FromQuery] Guid workflowRunId, CancellationToken ct) =>
        Ok(await sender.Send(new GetSupervisorDecisionsQuery(workflowRunId), ct));

    [HttpGet("summary")]
    public async Task<ActionResult<SupervisorSummaryDto>> GetSummary(
        [FromQuery] Guid workspaceId, [FromQuery] int limit, CancellationToken ct) =>
        Ok(await sender.Send(new GetSupervisorSummaryQuery(workspaceId, limit == 0 ? 100 : limit), ct));
}
