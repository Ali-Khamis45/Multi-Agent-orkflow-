using System.Text;
using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Gmail API. OAuth2. https://developers.google.com/gmail/api/reference/rest</summary>
public sealed class GmailConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "gmail";
    public string DisplayName => "Gmail";
    public string Description => "Send reports and notifications on the founder's behalf.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://accounts.google.com/o/oauth2/v2/auth",
        "https://oauth2.googleapis.com/token",
        ["https://www.googleapis.com/auth/gmail.send"],
        "Connectors:Google:ClientId", "Connectors:Google:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => [];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("SendEmail", "Send email", "Sends an email from the connected Gmail account."),
    ];
    public IReadOnlyList<string> Events => [];

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://gmail.googleapis.com/gmail/v1/users/me/profile");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.Require("access_token"));
                var (ok, body) = await SendAsync(http, request, ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Gmail account.")
                    : new ConnectorHealthResult(false, $"Gmail returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Gmail: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        Task.FromResult(new ConnectorSyncResult(true, "Gmail has no company-memory-relevant data to sync — it's an action-only connector."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "SendEmail")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var to = input.RootElement.TryGetProperty("to", out var t) ? t.GetString() : null;
                var subject = input.RootElement.TryGetProperty("subject", out var s) ? s.GetString() : "";
                var bodyText = input.RootElement.TryGetProperty("body", out var b) ? b.GetString() : "";
                if (string.IsNullOrEmpty(to))
                    return new ConnectorActionResult(false, "{}", "Missing required input field 'to'.");

                var mime = $"To: {to}\r\nSubject: {subject}\r\nContent-Type: text/plain; charset=UTF-8\r\n\r\n{bodyText}";
                var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(mime)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

                var request = new HttpRequestMessage(HttpMethod.Post, "https://gmail.googleapis.com/gmail/v1/users/me/messages/send")
                {
                    Content = JsonBody(new { raw }),
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.Require("access_token"));
                var (ok, respBody) = await SendAsync(http, request, ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"message":"[MOCK] Email sent.","reason":"{{err}}"}"""));
    }
}
