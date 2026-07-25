namespace AiAgentsTeam.Infrastructure.Auth;

/// <summary>Configuration Layer (Phase 1.5 §9) — bound from appsettings/env, never
/// hardcoded. The default Secret below is a local-dev-only value, same treatment as
/// the documented default Postgres password (see docs/DEPLOYMENT.md); it must be
/// overridden via Jwt__Secret in any non-local deployment.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = "local-dev-only-secret-change-me-before-any-real-deployment-32chars";
    public string Issuer { get; set; } = "ai-agents-team";
    public string Audience { get; set; } = "ai-agents-team-frontend";
    public int ExpiryMinutes { get; set; } = 60 * 24 * 7; // 7 days — no refresh-token flow yet, see docs/ROADMAP.md
}
