using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Stripe API (v1). API-key auth: a restricted or secret API key, sent as a
/// Bearer token. https://docs.stripe.com/api</summary>
public sealed class StripeConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "stripe";
    public string DisplayName => "Stripe";
    public string Description => "Revenue, charges, and payment links.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["secretKey"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreatePaymentLink", "Create payment link", "Creates a shareable Stripe payment link for a product/price."),
    ];
    public IReadOnlyList<string> Events => ["ChargeSucceeded", "SubscriptionCreated"];

    private const string BaseUrl = "https://api.stripe.com/v1/";

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, BaseUrl + path) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", c.Require("secretKey"));
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "balance"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Stripe account.")
                    : new ConnectorHealthResult(false, $"Stripe returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Stripe: simulated healthy account connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "charges?limit=20"), ct);
                if (!ok) return new ConnectorSyncResult(false, $"Stripe returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var total = doc.RootElement.GetProperty("data").EnumerateArray()
                    .Where(c => c.GetProperty("paid").GetBoolean())
                    .Sum(c => c.GetProperty("amount").GetInt64()) / 100m;
                return new ConnectorSyncResult(
                    true, $"Summed ${total:F2} across recent Stripe charges.",
                    CompanyProfileSection: "business",
                    CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = $"Stripe: ${total:F2} in recent charges as of the last sync." });
            },
            err => new ConnectorSyncResult(
                true, "[MOCK] Simulated Stripe sync — $4,280.00 in charges this month.",
                CompanyProfileSection: "business",
                CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = $"[MOCK] Stripe sync simulated (real call failed: {err})." }));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreatePaymentLink")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var priceId = input.RootElement.TryGetProperty("priceId", out var p) ? p.GetString() : null;
                if (string.IsNullOrEmpty(priceId))
                    return new ConnectorActionResult(false, "{}", "Missing required input field 'priceId'.");

                var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["line_items[0][price]"] = priceId, ["line_items[0][quantity]"] = "1" });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, "payment_links", form), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"url":"https://buy.stripe.com/mock_link","message":"[MOCK] Payment link created.","reason":"{{err}}"}"""));
    }
}
