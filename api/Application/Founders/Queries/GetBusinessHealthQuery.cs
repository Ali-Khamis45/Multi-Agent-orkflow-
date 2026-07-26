using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Founders.Common;
using AiAgentsTeam.Domain.Founders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Founders.Queries;

public sealed record CategoryHealthDto(string Category, int Score, IReadOnlyList<string> Present, IReadOnlyList<string> Missing, string Explanation);
public sealed record BusinessHealthDto(int OverallScore, IReadOnlyList<CategoryHealthDto> Categories);

public sealed record GetBusinessHealthQuery(Guid WorkspaceId) : IRequest<BusinessHealthDto>;

public sealed class GetBusinessHealthQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBusinessHealthQuery, BusinessHealthDto>
{
    public async Task<BusinessHealthDto> Handle(GetBusinessHealthQuery request, CancellationToken cancellationToken)
    {
        var profileJson = await db.CompanyProfiles
            .Where(p => p.WorkspaceId == request.WorkspaceId)
            .Select(p => p.ProfileJson)
            .FirstOrDefaultAsync(cancellationToken) ?? CompanyProfileJson.DefaultProfileJson;

        var health = BusinessHealthCalculator.Calculate(profileJson);
        return new BusinessHealthDto(
            health.OverallScore,
            health.Categories.Select(c => new CategoryHealthDto(c.Category, c.Score, c.Present, c.Missing, c.Explanation)).ToList());
    }
}
