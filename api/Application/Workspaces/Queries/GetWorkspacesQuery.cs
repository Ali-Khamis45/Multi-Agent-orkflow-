using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workspaces.Queries;

public sealed record WorkspaceDto(Guid Id, string Name, DateTimeOffset CreatedAt);

public sealed record GetWorkspacesQuery : IRequest<IReadOnlyCollection<WorkspaceDto>>;

public sealed class GetWorkspacesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkspacesQuery, IReadOnlyCollection<WorkspaceDto>>
{
    public async Task<IReadOnlyCollection<WorkspaceDto>> Handle(GetWorkspacesQuery request, CancellationToken cancellationToken)
    {
        return await db.Workspaces
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkspaceDto(w.Id, w.Name, w.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
