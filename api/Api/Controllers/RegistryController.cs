using AiAgentsTeam.Application.Registry.Commands;
using AiAgentsTeam.Application.Registry.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Dynamic Agent Registry endpoints (ARCHITECTURE.md §4.2).</summary>
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
    public async Task<IActionResult> Heartbeat(string name, CancellationToken ct)
    {
        var found = await sender.Send(new HeartbeatCommand(name), ct);
        return found ? NoContent() : NotFound();
    }

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyCollection<AgentDto>>> GetAgents(CancellationToken ct) =>
        Ok(await sender.Send(new GetAgentsQuery(), ct));
}
