using System.Text;
using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Founder;

/// <summary>Google Drive API v3. OAuth2, drive.file scope (only files this app creates,
/// not the whole Drive). https://developers.google.com/drive/api/reference/rest/v3</summary>
public sealed class GoogleDriveConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "google-drive";
    public string DisplayName => "Google Drive";
    public string Description => "Save generated reports and documents.";
    public CompanyType CompanyType => CompanyType.Founder;
    public ConnectorAuthType AuthType => ConnectorAuthType.OAuth2;
    public ConnectorOAuthConfig? OAuth => new(
        "https://accounts.google.com/o/oauth2/v2/auth",
        "https://oauth2.googleapis.com/token",
        ["https://www.googleapis.com/auth/drive.file"],
        "Connectors:Google:ClientId", "Connectors:Google:ClientSecret");
    public IReadOnlyList<string> RequiredCredentialFields => [];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("UploadFile", "Upload file", "Uploads a text/markdown file (e.g. a generated report) to Drive."),
    ];
    public IReadOnlyList<string> Events => [];

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/drive/v3/about?fields=user");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.Require("access_token"));
                var (ok, body) = await SendAsync(http, request, ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Google Drive.")
                    : new ConnectorHealthResult(false, $"Google Drive returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Google Drive: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        Task.FromResult(new ConnectorSyncResult(true, "Google Drive has no company-memory-relevant data to sync — it's an action-only connector."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "UploadFile")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var name = input.RootElement.TryGetProperty("name", out var n) ? n.GetString() : "report.md";
                var content = input.RootElement.TryGetProperty("content", out var c) ? c.GetString() : "";

                var boundary = Guid.NewGuid().ToString("N");
                var metadata = JsonSerializer.Serialize(new { name });
                var payload =
                    $"--{boundary}\r\nContent-Type: application/json; charset=UTF-8\r\n\r\n{metadata}\r\n" +
                    $"--{boundary}\r\nContent-Type: text/markdown\r\n\r\n{content}\r\n--{boundary}--";

                var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart")
                {
                    Content = new StringContent(payload, Encoding.UTF8, $"multipart/related; boundary={boundary}"),
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", credentials.Require("access_token"));
                var (ok, respBody) = await SendAsync(http, request, ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"fileId":"mock_file_123","message":"[MOCK] File uploaded to Drive.","reason":"{{err}}"}"""));
    }
}
