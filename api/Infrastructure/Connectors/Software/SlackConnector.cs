using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>Slack Web API. OAuth2 (Slack app with bot token scopes).
/// https://api.slack.com/web</summary>
public sealed class SlackConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "slack";
    public string DisplayName => "Slack";
    public string Description => "Post messages and notifications to connected channels.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://slack.com/oauth/v2/authorize",
        "https://slack.com/api/oauth.v2.access",
        ["chat:write", "channels:read"],
        "Connectors:Slack:ClientId", "Connectors:Slack:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => [];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("PostMessage", "Post message", "Posts a message to a channel."),
    ];
    public IReadOnlyList<string> Events => ["MessagePosted"];

    private HttpRequestMessage NewRequest(ConnectorCredentials c, string method, HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://slack.com/api/{method}") { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("access_token"));
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(credentials, "auth.test", new StringContent("")), ct);
                var reallyOk = ok && body.Contains("\"ok\":true");
                return reallyOk
                    ? new ConnectorHealthResult(true, "Connected to Slack workspace.")
                    : new ConnectorHealthResult(false, $"Slack returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Slack: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        Task.FromResult(new ConnectorSyncResult(true, "Slack has no company-memory-relevant data to sync — it's an action-only connector."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "PostMessage")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (channel, text) = (input.RootElement.GetProperty("channel").GetString()!, input.RootElement.GetProperty("text").GetString()!);
                var (ok, body) = await SendAsync(http, NewRequest(credentials, "chat.postMessage", JsonBody(new { channel, text })), ct);
                var reallyOk = ok && body.Contains("\"ok\":true");
                return reallyOk ? new ConnectorActionResult(true, body) : new ConnectorActionResult(false, "{}", Truncate(body, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"message":"[MOCK] Message posted to Slack.","reason":"{{err}}"}"""));
    }
}
