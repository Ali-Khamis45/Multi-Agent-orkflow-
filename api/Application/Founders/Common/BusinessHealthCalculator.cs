using System.Text.Json.Nodes;

namespace AiAgentsTeam.Application.Founders.Common;

public sealed record CategoryHealth(string Category, int Score, IReadOnlyList<string> Present, IReadOnlyList<string> Missing, string Explanation);
public sealed record BusinessHealth(int OverallScore, IReadOnlyList<CategoryHealth> Categories);

/// <summary>
/// Phase 3 "Business Health Engine" — a pure, deterministic function over whatever is
/// actually in the CompanyProfile. No score is ever invented: a field counts toward a
/// category only if the founder or an agent actually set it, and every category
/// explains exactly which fields are missing, in plain language, rather than returning
/// a bare number.
/// </summary>
public static class BusinessHealthCalculator
{
    private sealed record FieldCheck(string Section, string Field, string Label, bool IsList = false);

    private static readonly IReadOnlyDictionary<string, FieldCheck[]> CategoryFields = new Dictionary<string, FieldCheck[]>
    {
        ["Business Completeness"] =
        [
            new("basicInfo", "companyName", "company name"),
            new("basicInfo", "industry", "industry"),
            new("basicInfo", "businessType", "business type"),
            new("basicInfo", "country", "country"),
            new("basicInfo", "businessDescription", "business description"),
            new("business", "revenueModel", "revenue model"),
            new("business", "growthGoal", "growth goal"),
        ],
        ["Brand Completeness"] =
        [
            new("brand", "mission", "mission"),
            new("brand", "vision", "vision"),
            new("brand", "brandPersonality", "brand personality"),
            new("brand", "brandVoice", "brand voice"),
            new("brand", "slogan", "slogan"),
            new("brand", "coreValues", "core values", IsList: true),
        ],
        ["Marketing Readiness"] =
        [
            new("marketing", "channels", "marketing channels", IsList: true),
            new("marketing", "contentStyle", "content style"),
            new("marketing", "socialPlatforms", "social platforms", IsList: true),
        ],
        ["Financial Readiness"] =
        [
            new("business", "budget", "budget"),
            new("business", "fundingStatus", "funding status"),
            new("business", "monthlyRevenueGoal", "monthly revenue goal"),
        ],
        ["Launch Readiness"] =
        [
            new("business", "launchDate", "launch date"),
            new("competition", "competitors", "competitor analysis", IsList: true),
            new("marketing", "channels", "marketing channels", IsList: true),
            new("operations", "suppliers", "suppliers", IsList: true),
        ],
        ["Product Readiness"] =
        [
            new("products", "catalog", "product catalog", IsList: true),
            new("products", "categories", "product categories", IsList: true),
            new("products", "manufacturingStrategy", "manufacturing strategy"),
            new("products", "pricingStrategy", "pricing strategy"),
        ],
    };

    public static BusinessHealth Calculate(string profileJson)
    {
        var root = JsonNode.Parse(profileJson)!.AsObject();
        var categories = new List<CategoryHealth>();

        foreach (var (category, fields) in CategoryFields)
        {
            var present = new List<string>();
            var missing = new List<string>();

            foreach (var field in fields)
            {
                if (IsSet(root, field))
                    present.Add(field.Label);
                else
                    missing.Add(field.Label);
            }

            var score = fields.Length == 0 ? 0 : (int)Math.Round(present.Count * 100.0 / fields.Length);
            var explanation = missing.Count == 0
                ? $"All {fields.Length} {category.ToLowerInvariant()} field(s) are set."
                : $"{present.Count} of {fields.Length} set — missing: {string.Join(", ", missing)}.";

            categories.Add(new CategoryHealth(category, score, present, missing, explanation));
        }

        var overall = categories.Count == 0 ? 0 : (int)Math.Round(categories.Average(c => c.Score));
        return new BusinessHealth(overall, categories);
    }

    private static bool IsSet(JsonObject root, FieldCheck field)
    {
        var section = root[field.Section] as JsonObject;
        var value = section?[field.Field];
        if (value is null) return false;

        if (field.IsList)
            return value is JsonArray array && array.Count > 0;

        if (value is JsonValue scalar && scalar.TryGetValue<string>(out var s))
            return !string.IsNullOrWhiteSpace(s);

        return true; // present, non-string scalar (number/date/bool) or nested object
    }
}
