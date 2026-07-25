using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Workspaces;
using MediatR;

namespace AiAgentsTeam.Application.Workspaces.Commands;

/// <summary>UserId is null only for the AI runtime's own service-to-service
/// bootstrap call (see WorkspacesController) — every user-initiated workspace is
/// owned.</summary>
public sealed record CreateWorkspaceCommand(string Name, Guid? UserId) : IRequest<Guid>;

public sealed class CreateWorkspaceCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateWorkspaceCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = new Workspace(request.Name, request.UserId);
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return workspace.Id;
    }
}
