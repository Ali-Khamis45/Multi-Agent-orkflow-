using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>WooCommerce REST API v3 (WordPress plugin). API-key auth: a store-generated
/// consumer key/secret pair. https://woocommerce.github.io/woocommerce-rest-api-docs</summary>
public sealed class WooCommerceConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "woocommerce";
    public string DisplayName => "WooCommerce";
    public string Description => "Products and orders from your WooCommerce store.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["storeUrl", "consumerKey", "consumerSecret"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateProduct", "Create product", "Adds a new product to the store's catalog."),
    ];
    public IReadOnlyList<string> Events => ["OrderCreated", "ProductUpdated"];

    private static string Url(ConnectorCredentials c, string path) =>
        $"https://{c.Require("storeUrl")}/wp-json/wc/v3/{path}" +
        $"{(path.Contains('?') ? '&' : '?')}consumer_key={Uri.EscapeDataString(c.Require("consumerKey"))}&consumer_secret={Uri.EscapeDataString(c.Require("consumerSecret"))}";

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, new HttpRequestMessage(HttpMethod.Get, Url(credentials, "products?per_page=1")), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to WooCommerce store.")
                    : new ConnectorHealthResult(false, $"WooCommerce returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] WooCommerce: simulated healthy store connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await SendAsync(http, new HttpRequestMessage(HttpMethod.Get, Url(credentials, "orders?per_page=1")), ct);
                if (!ok) return new ConnectorSyncResult(false, $"WooCommerce returned an error: {Truncate(body, 200)}");
                return new ConnectorSyncResult(
                    true, "Synced recent WooCommerce orders.",
                    CompanyProfileSection: "products",
                    CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = "WooCommerce store synced — recent orders retrieved." });
            },
            err => new ConnectorSyncResult(
                true, "[MOCK] Simulated WooCommerce sync — 8 products, 21 orders this month.",
                CompanyProfileSection: "products",
                CompanyProfilePatch: new Dictionary<string, object?> { ["notes"] = $"[MOCK] WooCommerce sync simulated (real call failed: {err})." }));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreateProduct")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var name = input.RootElement.TryGetProperty("name", out var n) ? n.GetString() : "New product";
                var body = JsonBody(new { name, type = "simple" });
                var (ok, respBody) = await SendAsync(http, new HttpRequestMessage(HttpMethod.Post, Url(credentials, "products")) { Content = body }, ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"message":"[MOCK] Product created in simulated WooCommerce store.","reason":"{{err}}"}"""));
    }
}
