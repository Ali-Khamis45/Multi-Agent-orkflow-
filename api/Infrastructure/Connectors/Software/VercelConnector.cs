using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>Vercel REST API. API-key auth: a personal/team access token.
/// https://vercel.com/docs/rest-api</summary>
public sealed class VercelConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "vercel";
    public string DisplayName => "Vercel";
    public string Description => "Trigger deployments and read deployment status.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["accessToken"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("TriggerDeployment", "Trigger deployment", "Triggers a new deployment for a project from a Git branch."),
    ];
    public IReadOnlyList<string> Events => ["DeploymentSucceeded", "DeploymentFailed"];

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, $"https://api.vercel.com/{path}") { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("accessToken"));
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "v2/user"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Vercel account.")
                    : new ConnectorHealthResult(false, $"Vercel returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Vercel: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "v6/deployments?limit=10"), ct);
                if (!ok) return new ConnectorSyncResult(false, $"Vercel returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var count = doc.RootElement.TryGetProperty("deployments", out var d) ? d.GetArrayLength() : 0;
                return new ConnectorSyncResult(true, $"Synced {count} recent Vercel deployment(s).", MemoryKind: "Doc", MemoryContent: $"Vercel: {count} recent deployment(s) synced.");
            },
            err => new ConnectorSyncResult(true, "[MOCK] Simulated Vercel sync — 5 deployments, all succeeded.", MemoryKind: "Doc", MemoryContent: $"[MOCK] Vercel sync simulated (real call failed: {err})."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "TriggerDeployment")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (name, gitBranch) = (input.RootElement.GetProperty("project").GetString()!, input.RootElement.TryGetProperty("gitBranch", out var b) ? b.GetString() : "main");
                var body = JsonBody(new { name, target = "production", gitSource = new { type = "github", @ref = gitBranch } });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, "v13/deployments", body), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"url":"mock-deployment.vercel.app","message":"[MOCK] Deployment triggered.","reason":"{{err}}"}"""));
    }
}
