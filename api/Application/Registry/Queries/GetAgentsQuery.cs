using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Registry.Queries;

/// <summary>Scoped by CompanyType (Phase 2) — a Founder-workspace user never sees
/// Software-company agents and vice versa.</summary>
public sealed record GetAgentsQuery(CompanyType CompanyType) : IRequest<IReadOnlyCollection<AgentDto>>;

public sealed record AgentDto(
    string Name, string CompanyType, string Version, string Description, List<string> Skills,
    List<string> SupportedTasks, int Priority, string Status, int InFlightTaskCount,
    DateTimeOffset LastHeartbeatAt);

public sealed class GetAgentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAgentsQuery, IReadOnlyCollection<AgentDto>>
{
    public async Task<IReadOnlyCollection<AgentDto>> Handle(GetAgentsQuery request, CancellationToken cancellationToken)
    {
        return await db.AgentRegistrations
            .Where(a => a.CompanyType == request.CompanyType)
            .Select(a => new AgentDto(
                a.Name, a.CompanyType.ToString(), a.Version, a.Description, a.Skills, a.SupportedTasks,
                a.Priority, a.Status.ToString(), a.InFlightTaskCount, a.LastHeartbeatAt))
            .ToListAsync(cancellationToken);
    }
}
