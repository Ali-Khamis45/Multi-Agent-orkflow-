using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Registry.Commands;

/// <summary>Agent heartbeat (ARCHITECTURE.md §4.2 step 3).</summary>
public sealed record HeartbeatCommand(string Name) : IRequest<bool>;

public sealed class HeartbeatCommandHandler(IApplicationDbContext db) : IRequestHandler<HeartbeatCommand, bool>
{
    public async Task<bool> Handle(HeartbeatCommand request, CancellationToken cancellationToken)
    {
        var agent = await db.AgentRegistrations.FirstOrDefaultAsync(a => a.Name == request.Name, cancellationToken);
        if (agent is null) return false;

        agent.Heartbeat();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
