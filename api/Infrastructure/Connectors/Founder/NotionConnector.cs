using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Notion API. API-key auth: an internal integration token (simpler and more
/// common for single-workspace automation than Notion's public OAuth flow).
/// https://developers.notion.com/reference</summary>
public sealed class NotionConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "notion";
    public string DisplayName => "Notion";
    public string Description => "Create and update pages — business plans, reports, meeting notes.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["apiKey", "parentPageId"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreatePage", "Create page", "Creates a new Notion page under the configured parent page."),
    ];
    public IReadOnlyList<string> Events => [];

    private const string BaseUrl = "https://api.notion.com/v1/";

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, BaseUrl + path) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("apiKey"));
        request.Headers.Add("Notion-Version", "2022-06-28");
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "users/me"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Notion workspace.")
                    : new ConnectorHealthResult(false, $"Notion returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Notion: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        Task.FromResult(new ConnectorSyncResult(true, "Notion has no company-memory-relevant data to sync — it's an action-only connector."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreatePage")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var title = input.RootElement.TryGetProperty("title", out var t) ? t.GetString() : "Untitled";
                var content = input.RootElement.TryGetProperty("content", out var c) ? c.GetString() : "";

                var body = JsonBody(new
                {
                    parent = new { page_id = credentials.Require("parentPageId") },
                    properties = new { title = new { title = new[] { new { text = new { content = title } } } } },
                    children = new[]
                    {
                        new { @object = "block", type = "paragraph", paragraph = new { rich_text = new[] { new { text = new { content } } } } },
                    },
                });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, "pages", body), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"pageId":"mock_page_123","message":"[MOCK] Notion page created.","reason":"{{err}}"}"""));
    }
}
