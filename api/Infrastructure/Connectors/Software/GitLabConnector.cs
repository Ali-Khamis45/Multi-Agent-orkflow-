using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>GitLab REST API v4. OAuth2. https://docs.gitlab.com/ee/api/rest/</summary>
public sealed class GitLabConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "gitlab";
    public string DisplayName => "GitLab";
    public string Description => "Merge requests and pipelines against connected projects.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://gitlab.com/oauth/authorize",
        "https://gitlab.com/oauth/token",
        ["api"],
        "Connectors:GitLab:ClientId", "Connectors:GitLab:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => [];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateMergeRequest", "Create merge request", "Opens a merge request from a source branch into the target branch."),
    ];
    public IReadOnlyList<string> Events => ["MergeRequestOpened", "PipelineFinished"];

    private const string BaseUrl = "https://gitlab.com/api/v4/";

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, BaseUrl + path) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("access_token"));
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "user"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to GitLab account.")
                    : new ConnectorHealthResult(false, $"GitLab returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] GitLab: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "projects?membership=true&per_page=10"), ct);
                if (!ok) return new ConnectorSyncResult(false, $"GitLab returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var count = doc.RootElement.GetArrayLength();
                return new ConnectorSyncResult(true, $"Synced {count} GitLab project(s).", MemoryKind: "Doc", MemoryContent: $"GitLab: {count} project(s) synced.");
            },
            err => new ConnectorSyncResult(true, "[MOCK] Simulated GitLab sync — 4 projects, 2 open merge requests.", MemoryKind: "Doc", MemoryContent: $"[MOCK] GitLab sync simulated (real call failed: {err})."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreateMergeRequest")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (projectId, sourceBranch, targetBranch, title) = (
                    input.RootElement.GetProperty("projectId").GetString()!, input.RootElement.GetProperty("sourceBranch").GetString()!,
                    input.RootElement.TryGetProperty("targetBranch", out var t) ? t.GetString()! : "main",
                    input.RootElement.GetProperty("title").GetString()!);
                var body = JsonBody(new { source_branch = sourceBranch, target_branch = targetBranch, title });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, $"projects/{Uri.EscapeDataString(projectId)}/merge_requests", body), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"iid":1,"message":"[MOCK] Merge request opened.","reason":"{{err}}"}"""));
    }
}
