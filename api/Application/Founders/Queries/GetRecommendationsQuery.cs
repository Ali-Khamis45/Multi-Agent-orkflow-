using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Founders.Common;
using AiAgentsTeam.Domain.Founders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Founders.Queries;

public sealed record RecommendationDto(string Category, string Text, int CategoryScore);

/// <summary>Phase 3 "Recommendation Engine" — every recommendation is a direct
/// restatement of a real CompanyProfile gap from the Business Health Engine
/// (<see cref="BusinessHealthCalculator"/>); nothing here is templated marketing copy
/// unconnected to the founder's actual data.</summary>
public sealed record GetRecommendationsQuery(Guid WorkspaceId, int Limit = 5) : IRequest<IReadOnlyList<RecommendationDto>>;

public sealed class GetRecommendationsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRecommendationsQuery, IReadOnlyList<RecommendationDto>>
{
    public async Task<IReadOnlyList<RecommendationDto>> Handle(GetRecommendationsQuery request, CancellationToken cancellationToken)
    {
        var profileJson = await db.CompanyProfiles
            .Where(p => p.WorkspaceId == request.WorkspaceId)
            .Select(p => p.ProfileJson)
            .FirstOrDefaultAsync(cancellationToken) ?? CompanyProfileJson.DefaultProfileJson;

        var health = BusinessHealthCalculator.Calculate(profileJson);

        var recommendations = health.Categories
            .Where(c => c.Missing.Count > 0)
            .OrderBy(c => c.Score)
            .SelectMany(c => c.Missing.Select(field => new RecommendationDto(
                c.Category,
                $"Add your {field} to strengthen {c.Category}.",
                c.Score)))
            .Take(request.Limit)
            .ToList();

        if (recommendations.Count == 0)
        {
            recommendations.Add(new RecommendationDto(
                "Overall", "Your Company Profile is fully filled in — ask your AI team for the next move.", 100));
        }

        return recommendations;
    }
}
