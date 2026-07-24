using AiAgentsTeam.Application.Supervisor.Commands;
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
}
