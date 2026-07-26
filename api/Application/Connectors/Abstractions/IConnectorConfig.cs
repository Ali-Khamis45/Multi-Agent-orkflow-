namespace AiAgentsTeam.Application.Connectors.Abstractions;

/// <summary>Thin read-only config lookup so Application-layer OAuth handlers never take
/// a direct dependency on Microsoft.Extensions.Configuration (an Infrastructure/hosting
/// concern) — implemented in Infrastructure as a one-line wrapper over IConfiguration,
/// same reason IApplicationDbContext exists instead of injecting EF Core's DbContext
/// directly into Application handlers.</summary>
public interface IConnectorConfig
{
    string? Get(string key);
    string Require(string key);
}
