using AiAgentsTeam.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>
/// The dashboard's "submit a request and watch it run" entry point (Phase 1.6).
/// Proxies server-to-server to the AI Runtime's own /intake (the Supervisor's
/// kickoff) — the browser never talks to the AI Runtime directly.
/// </summary>
[ApiController]
[Route("api/intake")]
public sealed class IntakeController(IAiRuntimeClient aiRuntime) : ControllerBase
{
    public sealed record SubmitIntakeRequest(string RawInput, Guid? WorkspaceId);
    public sealed record SubmitIntakeResponse(Guid WorkflowRunId);

    [HttpPost]
    public async Task<ActionResult<SubmitIntakeResponse>> Submit(SubmitIntakeRequest request, CancellationToken ct)
    {
        var workflowRunId = await aiRuntime.SubmitIntakeAsync(request.RawInput, request.WorkspaceId, ct);
        return Ok(new SubmitIntakeResponse(workflowRunId));
    }
}
