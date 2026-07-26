namespace AiAgentsTeam.Application.Connectors.Abstractions;

/// <summary>The one genuinely generic piece of OAuth2 handling — the authorization-code
/// token exchange POST is the same shape across every provider's implementation of the
/// standard (RFC 6749 §4.1.3), so this is implemented exactly once in Infrastructure and
/// used by every OAuth2-type connector via CompleteConnectorOAuthCommand, instead of each
/// connector reimplementing the same HTTP POST.</summary>
public interface IOAuth2TokenExchanger
{
    Task<IReadOnlyDictionary<string, string>> ExchangeCodeAsync(
        ConnectorOAuthConfig config, string code, string redirectUri, CancellationToken ct);
}
