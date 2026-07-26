using AiAgentsTeam.Domain.Users;

namespace AiAgentsTeam.Application.Connectors.Abstractions;

public enum ConnectorAuthType { OAuth2, ApiKey }

/// <summary>Where to send the user and which config keys hold the app's own OAuth
/// client id/secret (never the literal secret — those live in configuration/
/// environment, exactly like Jwt:Secret and Internal:ServiceKey already do).</summary>
public sealed record ConnectorOAuthConfig(
    string AuthorizeUrl,
    string TokenUrl,
    IReadOnlyList<string> Scopes,
    string ClientIdConfigKey,
    string ClientSecretConfigKey);

public sealed record ConnectorActionDefinition(string Key, string DisplayName, string Description);

public sealed record ConnectorHealthResult(bool Healthy, string Message);

/// <summary>What a sync run found, and where it should land. A connector only ever
/// populates the pair matching its own CompanyType (Founder connectors populate the
/// CompanyProfile pair, Software connectors the Memory pair) — the generic
/// SyncConnectorCommand handler (Application/Connectors/Commands) applies whichever
/// pair is present without knowing which connector produced it, which is what keeps
/// connector-specific logic out of the core sync path.</summary>
public sealed record ConnectorSyncResult(
    bool Success,
    string Summary,
    string? CompanyProfileSection = null,
    IReadOnlyDictionary<string, object?>? CompanyProfilePatch = null,
    string? MemoryKind = null,
    string? MemoryContent = null);

public sealed record ConnectorActionResult(bool Success, string OutputJson, string? ErrorMessage = null);

/// <summary>Decrypted, ready-to-use credentials for one connector call — the connector
/// implementation decides what keys it needs (e.g. "accessToken" for OAuth2 connectors,
/// "apiKey"+"storeUrl" for Shopify's API-key mode) and documents them via
/// <see cref="IConnectorDefinition.RequiredCredentialFields"/>.</summary>
public sealed class ConnectorCredentials(IReadOnlyDictionary<string, string> values)
{
    public string? Get(string key) => values.TryGetValue(key, out var v) ? v : null;
    public string Require(string key) => Get(key) ?? throw new InvalidOperationException($"Missing required credential field '{key}'.");
}

/// <summary>
/// The Connector Framework's one plugin contract (Phase 4). Everything the platform
/// knows about a specific external system — Shopify, GitHub, Stripe, whatever comes
/// next — lives behind this interface. The core (registry, API controller, sync/action
/// command handlers) only ever depends on this interface, never on a concrete
/// connector, which is what "no connector-specific logic should leak into the core
/// architecture" means in practice: adding connector #19 is "implement this interface
/// and register it in DI," nothing else changes.
/// </summary>
public interface IConnectorDefinition
{
    string Key { get; }
    string DisplayName { get; }
    string Description { get; }
    CompanyType CompanyType { get; }
    ConnectorAuthType AuthType { get; }
    ConnectorOAuthConfig? OAuth { get; }
    IReadOnlyList<string> RequiredCredentialFields { get; }
    IReadOnlyList<ConnectorActionDefinition> Actions { get; }
    IReadOnlyList<string> Events { get; }

    Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct);
    Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct);
    Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct);
}
