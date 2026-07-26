using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Founders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Founders.Queries;

public sealed record CompanyProfileDto(Guid Id, Guid WorkspaceId, bool IsOnboarded, string ProfileJson, DateTimeOffset UpdatedAt);

/// <summary>Get-or-create — every Founder Workspace gets exactly one CompanyProfile, and
/// callers (frontend, ai-runtime agents) never need to special-case "no profile yet."
/// The first read for a workspace transparently creates the default-shaped, unonboarded
/// profile (see CompanyProfileJson.DefaultProfileJson).</summary>
public sealed record GetCompanyProfileQuery(Guid WorkspaceId) : IRequest<CompanyProfileDto>;

public sealed class GetCompanyProfileQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCompanyProfileQuery, CompanyProfileDto>
{
    public async Task<CompanyProfileDto> Handle(GetCompanyProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await db.CompanyProfiles.FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (profile is null)
        {
            profile = new CompanyProfile(request.WorkspaceId);
            db.CompanyProfiles.Add(profile);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new CompanyProfileDto(profile.Id, profile.WorkspaceId, profile.IsOnboarded, profile.ProfileJson, profile.UpdatedAt);
    }
}
