using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>Docker Hub Hub API v2. API-key auth: username + access token (used as the
/// password to Hub's own login endpoint, which issues a short-lived JWT for subsequent
/// calls — Docker Hub has no OAuth2 authorization-code flow for third-party apps).
/// https://docs.docker.com/reference/api/hub/latest/</summary>
public sealed class DockerHubConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "dockerhub";
    public string DisplayName => "Docker Hub";
    public string Description => "Repository tags and automated build status.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["username", "accessToken"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("ListTags", "List image tags", "Lists tags for a repository."),
    ];
    public IReadOnlyList<string> Events => ["ImagePushed"];

    private async Task<string?> LoginAsync(ConnectorCredentials c, CancellationToken ct)
    {
        var body = JsonBody(new { username = c.Require("username"), password = c.Require("accessToken") });
        var (ok, respBody) = await SendAsync(http, new HttpRequestMessage(HttpMethod.Post, "https://hub.docker.com/v2/users/login/") { Content = body }, ct);
        if (!ok) return null;
        using var doc = JsonDocument.Parse(respBody);
        return doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() : null;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var token = await LoginAsync(credentials, ct);
                return token is not null
                    ? new ConnectorHealthResult(true, "Connected to Docker Hub account.")
                    : new ConnectorHealthResult(false, "Docker Hub login failed — check username/access token.");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Docker Hub: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var token = await LoginAsync(credentials, ct) ?? throw new InvalidOperationException("Docker Hub login failed.");
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://hub.docker.com/v2/repositories/{credentials.Require("username")}/?page_size=10");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("JWT", token);
                var (ok, body) = await SendAsync(http, request, ct);
                if (!ok) return new ConnectorSyncResult(false, $"Docker Hub returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var count = doc.RootElement.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
                return new ConnectorSyncResult(true, $"Synced {count} Docker Hub repositories.", MemoryKind: "Doc", MemoryContent: $"Docker Hub: {count} repositories synced.");
            },
            err => new ConnectorSyncResult(true, "[MOCK] Simulated Docker Hub sync — 3 repositories.", MemoryKind: "Doc", MemoryContent: $"[MOCK] Docker Hub sync simulated (real call failed: {err})."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "ListTags")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var repo = input.RootElement.GetProperty("repository").GetString()!;
                var token = await LoginAsync(credentials, ct) ?? throw new InvalidOperationException("Docker Hub login failed.");
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://hub.docker.com/v2/repositories/{credentials.Require("username")}/{repo}/tags?page_size=10");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("JWT", token);
                var (ok, body) = await SendAsync(http, request, ct);
                return ok ? new ConnectorActionResult(true, body) : new ConnectorActionResult(false, "{}", Truncate(body, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"tags":["latest","v1.0.0"],"reason":"{{err}}"}"""));
    }
}
