namespace AiAgentsTeam.Api;

/// <summary>Shared secret for service-to-service calls that have no user session
/// (currently: the AI runtime's own startup Workspace bootstrap — see
/// WorkspacesController). Same local-dev-only-default treatment as Jwt:Secret and
/// the default Postgres password; must be overridden together with the AI
/// runtime's INTERNAL_SERVICE_KEY in any real deployment.</summary>
public sealed class InternalServiceOptions
{
    public const string SectionName = "Internal";

    public string ServiceKey { get; set; } = "local-dev-only-secret-change-me-before-any-real-deployment-32chars";
}
