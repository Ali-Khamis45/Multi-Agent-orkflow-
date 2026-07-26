using System.Text;
using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>GitHub REST API v3. OAuth2 (GitHub Apps/OAuth Apps).
/// https://docs.github.com/en/rest — the spec's signature example ("Fix issue #52 ->
/// clone -> branch -> modify -> commit -> PR") is realized here as CreateBranch +
/// CommitFile + OpenPullRequest; "clone" and "modify code" are the agent's own work
/// (via existing tools) before these two actions run.</summary>
public sealed class GitHubConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "github";
    public string DisplayName => "GitHub";
    public string Description => "Branches, commits, and pull requests against connected repositories.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://github.com/login/oauth/authorize",
        "https://github.com/login/oauth/access_token",
        ["repo"],
        "Connectors:GitHub:ClientId", "Connectors:GitHub:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => [];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateBranch", "Create branch", "Creates a new branch from the repository's default branch."),
        new("CommitFile", "Commit file", "Creates or updates one file on a branch."),
        new("OpenPullRequest", "Open pull request", "Opens a pull request from a branch into the default branch."),
    ];
    public IReadOnlyList<string> Events => ["IssueOpened", "PullRequestMerged"];

    private const string BaseUrl = "https://api.github.com/";

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, BaseUrl + path) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("access_token"));
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.UserAgent.ParseAdd("AiAgentsTeam-ConnectorFramework");
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "user"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to GitHub account.")
                    : new ConnectorHealthResult(false, $"GitHub returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] GitHub: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "user/repos?per_page=10&sort=updated"), ct);
                if (!ok) return new ConnectorSyncResult(false, $"GitHub returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var count = doc.RootElement.GetArrayLength();
                return new ConnectorSyncResult(true, $"Synced {count} recently updated repositories.", MemoryKind: "Doc", MemoryContent: $"GitHub: {count} recently updated repositories synced.");
            },
            err => new ConnectorSyncResult(true, "[MOCK] Simulated GitHub sync — 6 repositories, 3 open issues.", MemoryKind: "Doc", MemoryContent: $"[MOCK] GitHub sync simulated (real call failed: {err})."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct) =>
        actionKey switch
        {
            "CreateBranch" => CreateBranchAsync(credentials, inputJson, ct),
            "CommitFile" => CommitFileAsync(credentials, inputJson, ct),
            "OpenPullRequest" => OpenPullRequestAsync(credentials, inputJson, ct),
            _ => Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'.")),
        };

    private Task<ConnectorActionResult> CreateBranchAsync(ConnectorCredentials credentials, string inputJson, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (owner, repo, branch, fromSha) = (
                    input.RootElement.GetProperty("owner").GetString()!, input.RootElement.GetProperty("repo").GetString()!,
                    input.RootElement.GetProperty("branch").GetString()!, input.RootElement.GetProperty("fromSha").GetString()!);
                var body = JsonBody(new { @ref = $"refs/heads/{branch}", sha = fromSha });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, $"repos/{owner}/{repo}/git/refs", body), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"message":"[MOCK] Branch created.","reason":"{{err}}"}"""));

    private Task<ConnectorActionResult> CommitFileAsync(ConnectorCredentials credentials, string inputJson, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (owner, repo, path, branch, message, content) = (
                    input.RootElement.GetProperty("owner").GetString()!, input.RootElement.GetProperty("repo").GetString()!,
                    input.RootElement.GetProperty("path").GetString()!, input.RootElement.GetProperty("branch").GetString()!,
                    input.RootElement.GetProperty("message").GetString()!, input.RootElement.GetProperty("content").GetString()!);
                var body = JsonBody(new { message, content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)), branch });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Put, credentials, $"repos/{owner}/{repo}/contents/{path}", body), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"message":"[MOCK] File committed.","reason":"{{err}}"}"""));

    private Task<ConnectorActionResult> OpenPullRequestAsync(ConnectorCredentials credentials, string inputJson, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (owner, repo, title, head, baseBranch, bodyText) = (
                    input.RootElement.GetProperty("owner").GetString()!, input.RootElement.GetProperty("repo").GetString()!,
                    input.RootElement.GetProperty("title").GetString()!, input.RootElement.GetProperty("head").GetString()!,
                    input.RootElement.TryGetProperty("base", out var b) ? b.GetString()! : "main",
                    input.RootElement.TryGetProperty("body", out var d) ? d.GetString() : "");
                var body = JsonBody(new { title, head, @base = baseBranch, body = bodyText });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, $"repos/{owner}/{repo}/pulls", body), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"number":1,"html_url":"https://github.com/mock/mock/pull/1","message":"[MOCK] Pull request opened.","reason":"{{err}}"}"""));
}
