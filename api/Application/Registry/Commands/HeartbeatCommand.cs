using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Registry.Commands;

/// <summary>Agent heartbeat (ARCHITECTURE.md §4.2 step 3). Keyed by (Name,
/// CompanyType) since the same short name can be registered once per company.</summary>
public sealed record HeartbeatCommand(string Name, CompanyType CompanyType) : IRequest<bool>;

public sealed class HeartbeatCommandHandler(IApplicationDbContext db) : IRequestHandler<HeartbeatCommand, bool>
{
    public async Task<bool> Handle(HeartbeatCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.AgentRegistrations
            .FirstOrDefaultAsync(a => a.Name == request.Name && a.CompanyType == request.CompanyType, cancellationToken);
        if (agent is null) return false;

        agent.Heartbeat();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
