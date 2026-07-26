using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Google Ads API. OAuth2 — real usage also requires a developer token issued
/// per-account by Google Ads (not just OAuth), supplied here as a credential field
/// alongside the OAuth-issued access token. https://developers.google.com/google-ads/api</summary>
public sealed class GoogleAdsConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "google-ads";
    public string DisplayName => "Google Ads";
    public string Description => "Campaign performance and spend from a Google Ads account.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://accounts.google.com/o/oauth2/v2/auth",
        "https://oauth2.googleapis.com/token",
        ["https://www.googleapis.com/auth/adwords"],
        "Connectors:Google:ClientId", "Connectors:Google:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => ["customerId", "developerToken"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("GetCampaignPerformance", "Get campaign performance", "Reads spend and conversions for active campaigns."),
    ];
    public IReadOnlyList<string> Events => ["CampaignPerformanceUpdated"];

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, $"https://googleads.googleapis.com/v16/{path}") { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("access_token"));
        request.Headers.Add("developer-token", c.Require("developerToken"));
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "customers:listAccessibleCustomers"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Google Ads account.")
                    : new ConnectorHealthResult(false, $"Google Ads returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Google Ads: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var customerId = credentials.Require("customerId");
                var body = JsonBody(new { query = "SELECT campaign.name, metrics.cost_micros, metrics.conversions FROM campaign WHERE segments.date DURING LAST_30_DAYS" });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, $"customers/{customerId}/googleAds:search", body), ct);
                if (!ok) return new ConnectorSyncResult(false, $"Google Ads returned an error: {Truncate(respBody, 200)}");
                return new ConnectorSyncResult(
                    true, "Synced Google Ads campaign performance.",
                    CompanyProfileSection: "marketing",
                    CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = "Google Ads campaign performance synced." });
            },
            err => new ConnectorSyncResult(
                true, "[MOCK] Simulated Google Ads sync — 2 active campaigns, $340 spend, 18 conversions (last 30 days).",
                CompanyProfileSection: "marketing",
                CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = $"[MOCK] Google Ads sync simulated (real call failed: {err})." }));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "GetCampaignPerformance")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                var syncResult = await SyncAsync(credentials, ct);
                return new ConnectorActionResult(true, JsonSerializer.Serialize(new { summary = syncResult.Summary }));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"spend":340,"conversions":18,"reason":"{{err}}"}"""));
    }
}
