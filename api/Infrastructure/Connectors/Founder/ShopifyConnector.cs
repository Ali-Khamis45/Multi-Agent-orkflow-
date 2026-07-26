using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Shopify Admin REST API (2024-01). API-key auth: a custom app's Admin API
/// access token, scoped to one store. https://shopify.dev/docs/api/admin-rest</summary>
public sealed class ShopifyConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "shopify";
    public string DisplayName => "Shopify";
    public string Description => "Products, orders, and customers from your Shopify store.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["storeUrl", "accessToken"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateProduct", "Create product", "Adds a new product to the store's catalog."),
    ];
    public IReadOnlyList<string> Events => ["OrderCreated", "ProductUpdated"];

    private static string BaseUrl(ConnectorCredentials c) => $"https://{c.Require("storeUrl")}/admin/api/2024-01/";

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, BaseUrl(c) + path) { Content = content };
        request.Headers.Add("X-Shopify-Access-Token", c.Require("accessToken"));
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "shop.json"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Shopify store.")
                    : new ConnectorHealthResult(false, $"Shopify returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Shopify: simulated healthy store connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "products/count.json"), ct);
                if (!ok) return new ConnectorSyncResult(false, $"Shopify returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var count = doc.RootElement.GetProperty("count").GetInt32();
                return new ConnectorSyncResult(
                    true, $"Found {count} product(s) in Shopify.",
                    CompanyProfileSection: "products",
                    CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = $"Shopify store has {count} product(s) as of the last sync." });
            },
            err => new ConnectorSyncResult(
                true, "[MOCK] Simulated Shopify sync — 12 products, 34 orders this month.",
                CompanyProfileSection: "products",
                CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = $"[MOCK] Shopify sync simulated (real call failed: {err})." }));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreateProduct")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var title = input.RootElement.TryGetProperty("title", out var t) ? t.GetString() : "New product";
                var body = JsonBody(new { product = new { title } });
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, "products.json", body), ct);
                return ok
                    ? new ConnectorActionResult(true, respBody)
                    : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"message":"[MOCK] Product created in simulated Shopify store.","reason":"{{err}}"}"""));
    }
}
