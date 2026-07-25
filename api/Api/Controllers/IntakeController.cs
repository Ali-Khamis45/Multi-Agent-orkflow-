using System.Security.Claims;
using AiAgentsTeam.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>
/// The dashboard's "submit a request and watch it run" entry point (Phase 1.6).
/// Proxies server-to-server to the AI Runtime's own /intake (the Supervisor's
/// kickoff) — the browser never talks to the AI Runtime directly.
///
/// Phase 2 ("AI Enterprise OS"): this is also the platform's routing point
/// between companies — CompanyType is read from the caller's own JWT claim,
/// never accepted from the request body, so a request always executes under
/// the pipeline the authenticated user actually belongs to.
/// </summary>
[ApiController]
[Route("api/intake")]
[Authorize]
public sealed class IntakeController(IAiRuntimeClient aiRuntime) : ControllerBase
{
    public sealed record SubmitIntakeRequest(string RawInput, Guid? WorkspaceId);
    public sealed record SubmitIntakeResponse(Guid WorkflowRunId);

    [HttpPost]
    public async Task<ActionResult<SubmitIntakeResponse>> Submit(SubmitIntakeRequest request, CancellationToken ct)
    {
        var companyType = User.FindFirstValue("company_type") ?? "SoftwareCompany";
        var workflowRunId = await aiRuntime.SubmitIntakeAsync(request.RawInput, request.WorkspaceId, companyType, ct);
        return Ok(new SubmitIntakeResponse(workflowRunId));
    }
}
