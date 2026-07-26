using System.Text.Json.Nodes;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Founders.Queries;
using AiAgentsTeam.Domain.Founders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Founders.Commands;

/// <summary>Field-level merge-patch of one CompanyProfile section (Phase 3 "Smart
/// Agents" / Company Memory — see ai-runtime/app/agents/base.py's
/// `update_company_profile`). Retries on optimistic-concurrency conflicts because the
/// Founder Supervisor DAG genuinely runs parallel branches (e.g. Market Research +
/// Customer Research) that can race to patch the same CompanyProfile row at the same
/// moment — see CompanyProfileConfiguration's xmin concurrency token.</summary>
public sealed record PatchCompanyProfileSectionCommand(Guid WorkspaceId, string Section, string PatchJson) : IRequest<CompanyProfileDto>;

public sealed class PatchCompanyProfileSectionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<PatchCompanyProfileSectionCommand, CompanyProfileDto>
{
    private const int MaxAttempts = 5;

    public async Task<CompanyProfileDto> Handle(PatchCompanyProfileSectionCommand request, CancellationToken cancellationToken)
    {
        if (!CompanyProfileJson.Sections.Contains(request.Section))
            throw new ArgumentException($"Unknown CompanyProfile section '{request.Section}'.");

        var patchNode = JsonNode.Parse(request.PatchJson)
            ?? throw new ArgumentException("PatchJson must be valid JSON.");

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var profile = await db.CompanyProfiles.FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId, cancellationToken);
            if (profile is null)
            {
                profile = new CompanyProfile(request.WorkspaceId);
                db.CompanyProfiles.Add(profile);
            }

            profile.ApplySectionPatch(request.Section, patchNode.DeepClone());

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return new CompanyProfileDto(profile.Id, profile.WorkspaceId, profile.IsOnboarded, profile.ProfileJson, profile.UpdatedAt);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                // Another request patched (a different section of) this row between our
                // read and write — evict the now-stale tracked instance so the next
                // loop iteration's query actually re-hits the database, then reapply
                // just this patch on top of the fresh row.
                db.Detach(profile);
            }
        }

        throw new InvalidOperationException(
            $"Could not patch CompanyProfile section '{request.Section}' after {MaxAttempts} attempts due to concurrent writes.");
    }
}
