using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Workspaces;
using MediatR;

namespace AiAgentsTeam.Application.Workspaces.Commands;

public sealed record CreateWorkspaceCommand(string Name) : IRequest<Guid>;

public sealed class CreateWorkspaceCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateWorkspaceCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = new Workspace(request.Name);
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return workspace.Id;
    }
}
