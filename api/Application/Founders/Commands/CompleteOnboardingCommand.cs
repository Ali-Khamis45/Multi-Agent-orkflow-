using System.Text.Json.Nodes;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Founders.Queries;
using AiAgentsTeam.Domain.Founders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Founders.Commands;

/// <summary>The AI onboarding wizard's final step (Phase 3) — replaces the whole
/// CompanyProfile at once from the synthesized answers and marks the workspace
/// onboarded, so the Founder Dashboard never shows an empty state again and the
/// Supervisor's Dynamic Work router (see ai-runtime/app/supervisor/founder_router.py)
/// knows this workspace is past first-time setup.</summary>
public sealed record CompleteOnboardingCommand(Guid WorkspaceId, string ProfileJson) : IRequest<CompanyProfileDto>;

public sealed class CompleteOnboardingCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CompleteOnboardingCommand, CompanyProfileDto>
{
    public async Task<CompanyProfileDto> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var fullProfile = JsonNode.Parse(request.ProfileJson) ?? throw new ArgumentException("ProfileJson must be valid JSON.");

        var profile = await db.CompanyProfiles.FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId, cancellationToken);
        if (profile is null)
        {
            profile = new CompanyProfile(request.WorkspaceId);
            db.CompanyProfiles.Add(profile);
        }

        profile.CompleteOnboarding(fullProfile);
        await db.SaveChangesAsync(cancellationToken);

        return new CompanyProfileDto(profile.Id, profile.WorkspaceId, profile.IsOnboarded, profile.ProfileJson, profile.UpdatedAt);
    }
}
