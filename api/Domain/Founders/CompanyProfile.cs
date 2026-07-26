using System.Text.Json.Nodes;
using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Founders;

/// <summary>
/// Phase 3 ("AI Company Operating System") — the single source of truth for a Founder
/// Workspace's business. One per Workspace (1:1, enforced by a unique index — see
/// CompanyProfileConfiguration). Every Founder agent reads from this and writes back to
/// it (Company Memory, see ai-runtime/app/reasoning/pipeline.py), so the user never has
/// to re-explain their business.
///
/// Deliberately stored as a single JSONB blob (<see cref="ProfileJson"/>) rather than
/// ~8 owned-type tables with ~50 columns between them: the shape (see
/// <see cref="CompanyProfileJson"/> for the canonical default) is documented and typed
/// at the Application/frontend/AI-runtime boundaries via plain JSON, which is exactly
/// what every consumer (TypeScript, Python dicts, LLM-structured-output) already speaks
/// natively — a strongly-typed EF owned-type model would only need translating back to
/// JSON at every one of those boundaries anyway. New fields never require a migration.
/// </summary>
public class CompanyProfile : Entity
{
    public Guid WorkspaceId { get; private set; }
    public bool IsOnboarded { get; private set; }
    public string ProfileJson { get; private set; } = CompanyProfileJson.DefaultProfileJson;
    public DateTimeOffset UpdatedAt { get; private set; }

    private CompanyProfile() { }

    public CompanyProfile(Guid workspaceId)
    {
        WorkspaceId = workspaceId;
        ProfileJson = CompanyProfileJson.DefaultProfileJson;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Field-level (shallow) JSON merge-patch of one top-level section — e.g. an
    /// agent that only learned the company's `mission` and `slogan` doesn't overwrite
    /// `brandColors` set earlier by onboarding. Replacing the whole section wholesale
    /// would lose sibling fields no single agent run ever touches.</summary>
    public void ApplySectionPatch(string section, JsonNode patch)
    {
        if (!CompanyProfileJson.Sections.Contains(section))
            throw new ArgumentException($"Unknown CompanyProfile section '{section}'.", nameof(section));
        if (patch is not JsonObject patchObject)
            throw new ArgumentException("Patch must be a JSON object.", nameof(patch));

        var root = JsonNode.Parse(ProfileJson)!.AsObject();
        var current = root[section] as JsonObject ?? new JsonObject();
        foreach (var (key, value) in patchObject)
            current[key] = value?.DeepClone();
        root[section] = current;

        ProfileJson = root.ToJsonString();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Onboarding (Phase 3) replaces the whole profile at once — merged onto
    /// the default shape so a partial onboarding answer set still yields every section
    /// key, never a missing key downstream consumers would need to null-check for.</summary>
    public void CompleteOnboarding(JsonNode fullProfile)
    {
        if (fullProfile is not JsonObject fullObject)
            throw new ArgumentException("Profile must be a JSON object.", nameof(fullProfile));

        var root = JsonNode.Parse(CompanyProfileJson.DefaultProfileJson)!.AsObject();
        foreach (var section in CompanyProfileJson.Sections)
        {
            if (fullObject[section] is JsonObject sectionPatch)
            {
                var current = root[section]!.AsObject();
                foreach (var (key, value) in sectionPatch)
                    current[key] = value?.DeepClone();
            }
        }

        ProfileJson = root.ToJsonString();
        IsOnboarded = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
