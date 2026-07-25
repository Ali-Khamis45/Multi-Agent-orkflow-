namespace AiAgentsTeam.Domain.Users;

/// <summary>
/// Which specialized AI company a user operates (ARCHITECTURE_EXTENSION.md Phase 2,
/// "AI Enterprise OS"). Chosen once at registration, permanent for the account's
/// lifetime — a user who wants a different company creates a different account.
/// Deliberately distinct from <see cref="Domain.Workspaces.Workspace"/>, which keeps
/// its pre-existing, unrelated meaning (a named project/tenant container); this enum
/// decides which product — Mission Control vs. the Founder workspace — a user is
/// routed into, not which named workspace within that product they're working in.
/// </summary>
public enum CompanyType
{
    SoftwareCompany,
    Founder,
}
