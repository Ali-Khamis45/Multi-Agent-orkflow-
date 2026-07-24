using AiAgentsTeam.Application.Workspaces.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public sealed class WorkspacesController(ISender sender) : ControllerBase
{
    public sealed record CreateWorkspaceRequest(string Name);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWorkspaceRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateWorkspaceCommand(request.Name), ct);
        return CreatedAtAction(nameof(Create), new { id }, id);
    }
}
