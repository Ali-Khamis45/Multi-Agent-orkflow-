using System.Security.Claims;
using AiAgentsTeam.Application.Workspaces.Commands;
using AiAgentsTeam.Application.Workspaces.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public sealed class WorkspacesController(ISender sender) : ControllerBase
{
    public sealed record CreateWorkspaceRequest(string Name);

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWorkspaceRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateWorkspaceCommand(request.Name, CurrentUserId), ct);
        return CreatedAtAction(nameof(Create), new { id }, id);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkspaceDto>>> GetAll(CancellationToken ct) =>
        Ok(await sender.Send(new GetWorkspacesQuery(CurrentUserId), ct));
}
