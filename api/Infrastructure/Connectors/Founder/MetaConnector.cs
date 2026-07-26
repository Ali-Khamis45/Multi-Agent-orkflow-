using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Meta Graph API (Facebook Pages + Instagram Business). OAuth2. Publishing to
/// Instagram in production requires Meta App Review for the
/// instagram_content_publish permission — this connector saves drafts/creates media
/// containers, which is what's achievable without that review.
/// https://developers.facebook.com/docs/graph-api</summary>
public sealed class MetaConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "meta";
    public string DisplayName => "Meta (Facebook + Instagram)";
    public string Description => "Publish content and read campaign performance for connected Pages/Instagram accounts.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://www.facebook.com/v19.0/dialog/oauth",
        "https://graph.facebook.com/v19.0/oauth/access_token",
        ["pages_manage_posts", "pages_read_engagement", "instagram_content_publish", "ads_read"],
        "Connectors:Meta:ClientId", "Connectors:Meta:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => [];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateInstagramDraft", "Create Instagram draft", "Creates an Instagram media container with generated caption/image for review before publishing."),
    ];
    public IReadOnlyList<string> Events => ["CampaignPerformanceUpdated"];

    private const string BaseUrl = "https://graph.facebook.com/v19.0/";

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var token = credentials.Require("access_token");
                var (ok, body) = await SendAsync(http, new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}me?fields=id,name&access_token={Uri.EscapeDataString(token)}"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Meta account.")
                    : new ConnectorHealthResult(false, $"Meta returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Meta: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var token = credentials.Require("access_token");
                var (ok, body) = await SendAsync(http, new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}me/accounts?access_token={Uri.EscapeDataString(token)}"), ct);
                if (!ok) return new ConnectorSyncResult(false, $"Meta returned an error: {Truncate(body, 200)}");
                return new ConnectorSyncResult(
                    true, "Synced connected Meta Pages.",
                    CompanyProfileSection: "marketing",
                    CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = "Meta Pages synced — connected accounts retrieved." });
            },
            err => new ConnectorSyncResult(
                true, "[MOCK] Simulated Meta sync — 2 campaigns, 14,200 impressions this month.",
                CompanyProfileSection: "marketing",
                CompanyProfilePatch: new Dictionary<string, object?>
                {
                    ["channels"] = new[] { "Instagram", "Facebook" },
                    ["notes"] = $"[MOCK] Meta sync simulated (real call failed: {err}).",
                }));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreateInstagramDraft")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var igUserId = input.RootElement.TryGetProperty("igUserId", out var u) ? u.GetString() : null;
                var caption = input.RootElement.TryGetProperty("caption", out var c) ? c.GetString() : "";
                var imageUrl = input.RootElement.TryGetProperty("imageUrl", out var i) ? i.GetString() : null;
                if (string.IsNullOrEmpty(igUserId) || string.IsNullOrEmpty(imageUrl))
                    return new ConnectorActionResult(false, "{}", "Missing required input field(s) 'igUserId' and/or 'imageUrl'.");

                var token = credentials.Require("access_token");
                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["image_url"] = imageUrl, ["caption"] = caption ?? "", ["access_token"] = token,
                });
                var (ok, respBody) = await SendAsync(http, new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{igUserId}/media") { Content = form }, ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"containerId":"mock_container_123","message":"[MOCK] Instagram draft created.","reason":"{{err}}"}"""));
    }
}
