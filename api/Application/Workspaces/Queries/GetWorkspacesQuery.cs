using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workspaces.Queries;

public sealed record WorkspaceDto(Guid Id, string Name, DateTimeOffset CreatedAt);

/// <summary>Scoped to the authenticated caller (Phase 2 §"AI Enterprise OS") — a user
/// only ever sees their own Workspaces, never another account's.</summary>
public sealed record GetWorkspacesQuery(Guid UserId) : IRequest<IReadOnlyCollection<WorkspaceDto>>;

public sealed class GetWorkspacesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkspacesQuery, IReadOnlyCollection<WorkspaceDto>>
{
    public async Task<IReadOnlyCollection<WorkspaceDto>> Handle(GetWorkspacesQuery request, CancellationToken cancellationToken)
    {
        return await db.Workspaces
            .Where(w => w.UserId == request.UserId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkspaceDto(w.Id, w.Name, w.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
