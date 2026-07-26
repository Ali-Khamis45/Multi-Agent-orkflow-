using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>Linear API — GraphQL only, unlike every other connector in this catalog.
/// OAuth2. https://developers.linear.app/docs/graphql/working-with-the-graphql-api</summary>
public sealed class LinearConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "linear";
    public string DisplayName => "Linear";
    public string Description => "Create and track issues in connected Linear teams.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://linear.app/oauth/authorize",
        "https://api.linear.app/oauth/token",
        ["read", "write"],
        "Connectors:Linear:ClientId", "Connectors:Linear:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => [];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateIssue", "Create issue", "Creates a new issue in a Linear team."),
    ];
    public IReadOnlyList<string> Events => ["IssueStatusChanged"];

    private const string GraphQlUrl = "https://api.linear.app/graphql";

    private async Task<(bool Ok, string Body)> QueryAsync(ConnectorCredentials c, string query, object? variables, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GraphQlUrl) { Content = JsonBody(new { query, variables }) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("access_token"));
        return await SendAsync(http, request, ct);
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await QueryAsync(credentials, "{ viewer { id name } }", null, ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Linear workspace.")
                    : new ConnectorHealthResult(false, $"Linear returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Linear: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await QueryAsync(credentials, "{ issues(filter: { state: { type: { neq: \"completed\" } } }, first: 20) { nodes { id title } } }", null, ct);
                if (!ok) return new ConnectorSyncResult(false, $"Linear returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var count = doc.RootElement.GetProperty("data").GetProperty("issues").GetProperty("nodes").GetArrayLength();
                return new ConnectorSyncResult(true, $"Synced {count} open Linear issue(s).", MemoryKind: "Doc", MemoryContent: $"Linear: {count} open issue(s) synced.");
            },
            err => new ConnectorSyncResult(true, "[MOCK] Simulated Linear sync — 7 open issues.", MemoryKind: "Doc", MemoryContent: $"[MOCK] Linear sync simulated (real call failed: {err})."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreateIssue")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (teamId, title) = (input.RootElement.GetProperty("teamId").GetString()!, input.RootElement.GetProperty("title").GetString()!);
                const string mutation = "mutation($teamId: String!, $title: String!) { issueCreate(input: { teamId: $teamId, title: $title }) { success issue { id identifier } } }";
                var (ok, body) = await QueryAsync(credentials, mutation, new { teamId, title }, ct);
                return ok ? new ConnectorActionResult(true, body) : new ConnectorActionResult(false, "{}", Truncate(body, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"identifier":"MOCK-1","message":"[MOCK] Linear issue created.","reason":"{{err}}"}"""));
    }
}
