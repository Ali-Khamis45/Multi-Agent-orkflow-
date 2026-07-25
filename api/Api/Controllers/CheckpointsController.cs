using AiAgentsTeam.Application.Checkpoints.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Execution Snapshots (Phase 1.5 §5) — powers Mission Control's Execution Playback.</summary>
[ApiController]
[Route("api/checkpoints")]
public sealed class CheckpointsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CheckpointDto>>> GetForRun(
        [FromQuery] Guid workflowRunId, CancellationToken ct) =>
        Ok(await sender.Send(new GetCheckpointsQuery(workflowRunId), ct));
}
