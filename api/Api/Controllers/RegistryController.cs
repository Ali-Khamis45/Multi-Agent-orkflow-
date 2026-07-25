using System.Security.Claims;
using AiAgentsTeam.Application.Registry.Commands;
using AiAgentsTeam.Application.Registry.Queries;
using AiAgentsTeam.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Dynamic Agent Registry endpoints (ARCHITECTURE.md §4.2). Registration and
/// heartbeat are service-to-service calls from ai-runtime agent processes — no user
/// session exists there, so they stay unauthenticated and declare their own
/// CompanyType explicitly. Listing is browsed by a logged-in user, so it's scoped to
/// their CompanyType from the token instead of an unauthenticated query param.</summary>
[ApiController]
[Route("api/registry")]
public sealed class RegistryController(ISender sender) : ControllerBase
{
    [HttpPost("agents")]
    public async Task<ActionResult<Guid>> Register(RegisterAgentCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return Ok(id);
    }

    [HttpPut("agents/{name}/heartbeat")]
    public async Task<IActionResult> Heartbeat(string name, [FromQuery] CompanyType companyType, CancellationToken ct)
    {
        var found = await sender.Send(new HeartbeatCommand(name, companyType), ct);
        return found ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyCollection<AgentDto>>> GetAgents(CancellationToken ct)
    {
        var companyType = Enum.Parse<CompanyType>(User.FindFirstValue("company_type")!);
        return Ok(await sender.Send(new GetAgentsQuery(companyType), ct));
    }
}
