using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>Jira Cloud REST API v3 via Atlassian's OAuth 3LO. Atlassian's flow requires
/// looking up the Jira site's `cloudId` via the accessible-resources endpoint after
/// token exchange — captured here as a required credential field rather than an
/// implicit extra call, so it's visible/documented rather than hidden magic.
/// https://developer.atlassian.com/cloud/jira/platform/rest/v3/</summary>
public sealed class JiraConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "jira";
    public string DisplayName => "Jira";
    public string Description => "Create and track issues in connected Jira projects.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://auth.atlassian.com/authorize",
        "https://auth.atlassian.com/oauth/token",
        ["read:jira-work", "write:jira-work"],
        "Connectors:Jira:ClientId", "Connectors:Jira:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => ["cloudId"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateIssue", "Create issue", "Creates a new issue in a Jira project."),
    ];
    public IReadOnlyList<string> Events => ["IssueTransitioned"];

    private static string BaseUrl(ConnectorCredentials c) => $"https://api.atlassian.com/ex/jira/{c.Require("cloudId")}/rest/api/3/";

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, BaseUrl(c) + path) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("access_token"));
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "myself"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Jira site.")
                    : new ConnectorHealthResult(false, $"Jira returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Jira: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var body = JsonBody(new { jql = "assignee = currentUser() AND resolution = Unresolved", maxResults = 20 });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, "search", body), ct);
                if (!ok) return new ConnectorSyncResult(false, $"Jira returned an error: {Truncate(respBody, 200)}");
                using var doc = JsonDocument.Parse(respBody);
                var count = doc.RootElement.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
                return new ConnectorSyncResult(true, $"Synced {count} open Jira issue(s).", MemoryKind: "Doc", MemoryContent: $"Jira: {count} open issue(s) assigned to the connected account.");
            },
            err => new ConnectorSyncResult(true, "[MOCK] Simulated Jira sync — 5 open issues.", MemoryKind: "Doc", MemoryContent: $"[MOCK] Jira sync simulated (real call failed: {err})."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreateIssue")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (projectKey, summary, issueType) = (
                    input.RootElement.GetProperty("projectKey").GetString()!, input.RootElement.GetProperty("summary").GetString()!,
                    input.RootElement.TryGetProperty("issueType", out var it) ? it.GetString()! : "Task");
                var body = JsonBody(new { fields = new { project = new { key = projectKey }, summary, issuetype = new { name = issueType } } });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, "issue", body), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"key":"MOCK-1","message":"[MOCK] Jira issue created.","reason":"{{err}}"}"""));
    }
}
