using AiAgentsTeam.Application.Founders.Commands;
using AiAgentsTeam.Application.Founders.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>
/// Phase 3 ("AI Company Operating System") — the CompanyProfile is Company Memory's
/// source of truth. Read by both the frontend (a logged-in Founder, via JWT) and the
/// AI Runtime's agents (service-to-service, via the shared internal key — same dual-auth
/// pattern as WorkspacesController.Create) so every founder agent can build context from
/// it and write findings back to it without a user session existing at the time.
/// </summary>
[ApiController]
[Route("api/company-profile")]
public sealed class CompanyProfileController(ISender sender, IOptions<InternalServiceOptions> internalOptions) : ControllerBase
{
    public sealed record PatchSectionRequest(Guid WorkspaceId, string Section, object Patch);
    public sealed record CompleteOnboardingRequest(Guid WorkspaceId, object Profile);

    private const string ServiceKeyHeader = "X-Internal-Service-Key";

    private bool IsServiceOrAuthenticated =>
        (Request.Headers.TryGetValue(ServiceKeyHeader, out var key) && key == internalOptions.Value.ServiceKey)
        || User.Identity?.IsAuthenticated == true;

    [HttpGet]
    public async Task<ActionResult<CompanyProfileDto>> Get([FromQuery] Guid workspaceId, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        return Ok(await sender.Send(new GetCompanyProfileQuery(workspaceId), ct));
    }

    [HttpPatch("section")]
    public async Task<ActionResult<CompanyProfileDto>> PatchSection(PatchSectionRequest request, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        var patchJson = System.Text.Json.JsonSerializer.Serialize(request.Patch);
        return Ok(await sender.Send(new PatchCompanyProfileSectionCommand(request.WorkspaceId, request.Section, patchJson), ct));
    }

    [HttpPost("onboarding/complete")]
    public async Task<ActionResult<CompanyProfileDto>> CompleteOnboarding(CompleteOnboardingRequest request, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        var profileJson = System.Text.Json.JsonSerializer.Serialize(request.Profile);
        return Ok(await sender.Send(new CompleteOnboardingCommand(request.WorkspaceId, profileJson), ct));
    }

    [HttpGet("health")]
    public async Task<ActionResult<BusinessHealthDto>> GetHealth([FromQuery] Guid workspaceId, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        return Ok(await sender.Send(new GetBusinessHealthQuery(workspaceId), ct));
    }

    [HttpGet("timeline")]
    public async Task<ActionResult<IReadOnlyList<TimelineMilestoneDto>>> GetTimeline([FromQuery] Guid workspaceId, CancellationToken ct)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        return Ok(await sender.Send(new GetBusinessTimelineQuery(workspaceId), ct));
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<IReadOnlyList<RecommendationDto>>> GetRecommendations([FromQuery] Guid workspaceId, [FromQuery] int limit = 5, CancellationToken ct = default)
    {
        if (!IsServiceOrAuthenticated) return Unauthorized();
        return Ok(await sender.Send(new GetRecommendationsQuery(workspaceId, limit), ct));
    }
}
