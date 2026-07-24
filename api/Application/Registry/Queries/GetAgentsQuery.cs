using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Registry.Queries;

public sealed record GetAgentsQuery : IRequest<IReadOnlyCollection<AgentDto>>;

public sealed record AgentDto(
    string Name, string Version, string Description, List<string> Skills,
    List<string> SupportedTasks, int Priority, string Status, int InFlightTaskCount,
    DateTimeOffset LastHeartbeatAt);

public sealed class GetAgentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentsQuery, IReadOnlyCollection<AgentDto>>
{
    public async Task<IReadOnlyCollection<AgentDto>> Handle(GetAgentsQuery request, CancellationToken cancellationToken)
    {
        return await db.AgentRegistrations
            .Select(a => new AgentDto(
                a.Name, a.Version, a.Description, a.Skills, a.SupportedTasks,
                a.Priority, a.Status.ToString(), a.InFlightTaskCount, a.LastHeartbeatAt))
            .ToListAsync(cancellationToken);
    }
}
