namespace AiAgentsTeam.Domain.Founders;

/// <summary>
/// The canonical shape of <see cref="CompanyProfile.ProfileJson"/> — the one place this
/// shape is defined. TypeScript (frontend), Python (ai-runtime agents), and this API all
/// read/write exactly this JSON shape; there is no other schema to keep in sync.
///
/// Every section carries a <c>notes</c> field as a guaranteed-safe destination for
/// free-text agent output that didn't parse as structured JSON (see
/// ai-runtime/app/agents/base.py's `update_company_profile` — LLM-structured-output
/// extraction degrades to `notes` rather than corrupting a typed field or silently
/// dropping the agent's finding).
/// </summary>
public static class CompanyProfileJson
{
    public static readonly IReadOnlySet<string> Sections = new HashSet<string>
    {
        "basicInfo", "brand", "products", "customers", "business", "competition", "marketing", "operations",
    };

    public const string DefaultProfileJson = """
    {
      "basicInfo": { "companyName": null, "industry": null, "businessType": null, "country": null, "city": null, "launchStage": null, "businessDescription": null, "notes": null },
      "brand": { "mission": null, "vision": null, "coreValues": [], "brandPersonality": null, "brandVoice": null, "brandColors": [], "logoUrl": null, "slogan": null, "notes": null },
      "products": { "catalog": [], "categories": [], "manufacturingStrategy": null, "pricingStrategy": null, "notes": null },
      "customers": { "targetAudience": null, "personas": [], "problems": [], "goals": [], "notes": null },
      "business": { "revenueModel": null, "budget": null, "fundingStatus": null, "monthlyRevenueGoal": null, "growthGoal": null, "launchDate": null, "notes": null },
      "competition": { "competitors": [], "advantages": [], "weaknesses": [], "opportunities": [], "notes": null },
      "marketing": { "channels": [], "contentStyle": null, "socialPlatforms": [], "campaignHistory": [], "notes": null },
      "operations": { "suppliers": [], "inventoryStrategy": null, "shipping": null, "teamMembers": [], "notes": null }
    }
    """;
}
