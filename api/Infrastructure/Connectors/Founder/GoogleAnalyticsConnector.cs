using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Google Analytics Data API (GA4). OAuth2.
/// https://developers.google.com/analytics/devguides/reporting/data/v1</summary>
public sealed class GoogleAnalyticsConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "google-analytics";
    public string DisplayName => "Google Analytics";
    public string Description => "Website traffic and audience insights from a GA4 property.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://accounts.google.com/o/oauth2/v2/auth",
        "https://oauth2.googleapis.com/token",
        ["https://www.googleapis.com/auth/analytics.readonly"],
        "Connectors:Google:ClientId", "Connectors:Google:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => ["propertyId"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("GetTrafficReport", "Get traffic report", "Runs a report of sessions and users over the last 30 days."),
    ];
    public IReadOnlyList<string> Events => ["ReportReady"];

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://analyticsadmin.googleapis.com/v1beta/accountSummaries");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.Require("access_token"));
                var (ok, body) = await SendAsync(http, request, ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Google Analytics.")
                    : new ConnectorHealthResult(false, $"Google Analytics returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Google Analytics: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var propertyId = credentials.Require("propertyId");
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://analyticsdata.googleapis.com/v1beta/properties/{propertyId}:runReport")
                {
                    Content = JsonBody(new
                    {
                        dateRanges = new[] { new { startDate = "30daysAgo", endDate = "today" } },
                        metrics = new[] { new { name = "sessions" }, new { name = "activeUsers" } },
                    }),
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.Require("access_token"));
                var (ok, body) = await SendAsync(http, request, ct);
                if (!ok) return new ConnectorSyncResult(false, $"Google Analytics returned an error: {Truncate(body, 200)}");
                return new ConnectorSyncResult(
                    true, "Synced Google Analytics traffic report.",
                    CompanyProfileSection: "marketing",
                    CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = "Google Analytics traffic report synced." });
            },
            err => new ConnectorSyncResult(
                true, "[MOCK] Simulated GA4 sync — 3,240 sessions, 2,105 users (last 30 days).",
                CompanyProfileSection: "marketing",
                CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = $"[MOCK] Google Analytics sync simulated (real call failed: {err})." }));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "GetTrafficReport")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                var syncResult = await SyncAsync(credentials, ct);
                return new ConnectorActionResult(true, JsonSerializer.Serialize(new { summary = syncResult.Summary }));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"sessions":3240,"users":2105,"reason":"{{err}}"}"""));
    }
}
