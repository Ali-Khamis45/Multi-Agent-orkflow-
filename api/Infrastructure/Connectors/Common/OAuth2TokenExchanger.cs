using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AiAgentsTeam.Infrastructure.Connectors.Common;

/// <summary>Standard OAuth2 authorization-code token exchange (RFC 6749 §4.1.3) —
/// written once, correctly, against the spec every provider in the catalog implements;
/// unverified against any live provider in this environment (no registered OAuth app
/// exists to test against), unlike the framework code around it.</summary>
public sealed class OAuth2TokenExchanger(HttpClient http, IConfiguration configuration) : IOAuth2TokenExchanger
{
    public async Task<IReadOnlyDictionary<string, string>> ExchangeCodeAsync(
        ConnectorOAuthConfig config, string code, string redirectUri, CancellationToken ct)
    {
        var clientId = configuration[config.ClientIdConfigKey]
            ?? throw new InvalidOperationException($"Missing configuration '{config.ClientIdConfigKey}'.");
        var clientSecret = configuration[config.ClientSecretConfigKey]
            ?? throw new InvalidOperationException($"Missing configuration '{config.ClientSecretConfigKey}'.");

        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, config.TokenUrl) { Content = body };
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var result = new Dictionary<string, string>();
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString()!,
                JsonValueKind.Number => property.Value.GetRawText(),
                _ => property.Value.GetRawText(),
            };
        }
        return result;
    }
}
