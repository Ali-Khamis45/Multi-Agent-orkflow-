using System.Security.Claims;
using AiAgentsTeam.Application.Workspaces.Commands;
using AiAgentsTeam.Application.Workspaces.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiAgentsTeam.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public sealed class WorkspacesController(ISender sender, IOptions<InternalServiceOptions> internalOptions) : ControllerBase
{
    public sealed record CreateWorkspaceRequest(string Name);

    private const string ServiceKeyHeader = "X-Internal-Service-Key";

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Create is the one endpoint the AI runtime itself calls (to
    /// bootstrap its legacy default Workspace at startup) with no user session —
    /// so unlike every other action here, it isn't [Authorize]-gated at the
    /// attribute level. It accepts either a valid user JWT (normal registration/
    /// dashboard flow, workspace owned by that user) or the shared internal
    /// service key (system flow, workspace owned by nobody — Workspace.UserId is
    /// nullable specifically for this case).</summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWorkspaceRequest request, CancellationToken ct)
    {
        Guid? ownerId;
        if (Request.Headers.TryGetValue(ServiceKeyHeader, out var providedKey) &&
            providedKey == internalOptions.Value.ServiceKey)
        {
            ownerId = null;
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            ownerId = CurrentUserId;
        }
        else
        {
            return Unauthorized();
        }

        var id = await sender.Send(new CreateWorkspaceCommand(request.Name, ownerId), ct);
        return CreatedAtAction(nameof(Create), new { id }, id);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WorkspaceDto>>> GetAll(CancellationToken ct) =>
        Ok(await sender.Send(new GetWorkspacesQuery(CurrentUserId), ct));
}
