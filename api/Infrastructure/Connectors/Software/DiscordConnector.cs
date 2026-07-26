using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>Discord API v10. API-key auth: a bot token — Discord bots overwhelmingly
/// run as a static bot token added to a server, not a per-user OAuth2 flow, so ApiKey
/// is the realistic auth type here rather than OAuth2. https://discord.com/developers/docs</summary>
public sealed class DiscordConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "discord";
    public string DisplayName => "Discord";
    public string Description => "Post messages and notifications to a connected server.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["botToken"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("PostMessage", "Post message", "Posts a message to a channel."),
    ];
    public IReadOnlyList<string> Events => ["MessagePosted"];

    private const string BaseUrl = "https://discord.com/api/v10/";

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, BaseUrl + path) { Content = content };
        request.Headers.Add("Authorization", $"Bot {c.Require("botToken")}");
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "users/@me"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected as the configured Discord bot.")
                    : new ConnectorHealthResult(false, $"Discord returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Discord: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        Task.FromResult(new ConnectorSyncResult(true, "Discord has no company-memory-relevant data to sync — it's an action-only connector."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "PostMessage")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (channelId, content) = (input.RootElement.GetProperty("channelId").GetString()!, input.RootElement.GetProperty("content").GetString()!);
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, $"channels/{channelId}/messages", JsonBody(new { content })), ct);
                return ok ? new ConnectorActionResult(true, body) : new ConnectorActionResult(false, "{}", Truncate(body, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"message":"[MOCK] Message posted to Discord.","reason":"{{err}}"}"""));
    }
}
