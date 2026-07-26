using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Founders.Queries;

public sealed record TimelineMilestoneDto(string Title, string ArtifactName, DateTimeOffset At, string OwnerAgent);

/// <summary>Phase 3 "Business Timeline" — every milestone is a real Artifact this
/// workspace's AI team actually produced, at its real creation timestamp. No synthetic
/// "history" is generated; a workspace with no completed work simply has an empty
/// timeline.</summary>
public sealed record GetBusinessTimelineQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<TimelineMilestoneDto>>;

public sealed class GetBusinessTimelineQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetBusinessTimelineQuery, IReadOnlyList<TimelineMilestoneDto>>
{
    private static readonly IReadOnlyDictionary<string, string> MilestoneTitles = new Dictionary<string, string>
    {
        ["ExecutiveSummary"] = "Business Created",
        ["BusinessModelCanvas"] = "Business Model Defined",
        ["MarketResearchReport"] = "Market Research Completed",
        ["CustomerPersonas"] = "Customer Personas Defined",
        ["BrandIdentity"] = "Brand Identity Completed",
        ["FinancialProjection"] = "Financial Projection Completed",
        ["MarketingPlan"] = "Marketing Plan Generated",
        ["OperationsPlan"] = "Operations Plan Completed",
        ["SalesStrategy"] = "Sales Strategy Defined",
        ["GrowthRoadmap"] = "Growth Roadmap Completed",
        ["LaunchStrategy"] = "Launch Plan Ready",
    };

    public async Task<IReadOnlyList<TimelineMilestoneDto>> Handle(GetBusinessTimelineQuery request, CancellationToken cancellationToken)
    {
        var artifacts = await db.Artifacts
            .Where(a => a.WorkspaceId == request.WorkspaceId && MilestoneTitles.Keys.Contains(a.Name) && a.Version == 1)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new { a.Name, a.CreatedAt, a.OwnerAgent })
            .ToListAsync(cancellationToken);

        return artifacts
            .Select(a => new TimelineMilestoneDto(MilestoneTitles[a.Name], a.Name, a.CreatedAt, a.OwnerAgent))
            .ToList();
    }
}
