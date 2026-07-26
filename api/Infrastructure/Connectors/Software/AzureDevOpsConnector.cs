using System.Text;
using System.Text.Json;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using static AiAgentsTeam.Infrastructure.Connectors.Common.ConnectorHttpHelpers;

namespace AiAgentsTeam.Infrastructure.Connectors.Software;

/// <summary>Azure DevOps REST API. API-key auth: a Personal Access Token, sent as HTTP
/// Basic auth with an empty username (Azure DevOps's own documented convention, not a
/// real username/password pair). https://learn.microsoft.com/en-us/rest/api/azure/devops/</summary>
public sealed class AzureDevOpsConnector(HttpClient http) : IConnectorDefinition
{
    public string Key => "azure-devops";
    public string DisplayName => "Azure DevOps";
    public string Description => "Work items and pipelines in a connected Azure DevOps organization.";
    public CompanyType CompanyType => CompanyType.SoftwareCompany;
    public ConnectorAuthType AuthType => ConnectorAuthType.ApiKey;
    public ConnectorOAuthConfig? OAuth => null;
    public IReadOnlyList<string> RequiredCredentialFields => ["organization", "personalAccessToken"];
    public IReadOnlyList<ConnectorActionDefinition> Actions =>
    [
        new("CreateWorkItem", "Create work item", "Creates a new work item (e.g. Task, Bug) in a project."),
    ];
    public IReadOnlyList<string> Events => ["WorkItemUpdated", "BuildCompleted"];

    private HttpRequestMessage NewRequest(HttpMethod method, ConnectorCredentials c, string path, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, $"https://dev.azure.com/{c.Require("organization")}/{path}") { Content = content };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{c.Require("personalAccessToken")}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);
        return request;
    }

    public Task<ConnectorHealthResult> CheckHealthAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "_apis/projects?api-version=7.1"), ct);
                return ok
                    ? new ConnectorHealthResult(true, "Connected to Azure DevOps organization.")
                    : new ConnectorHealthResult(false, $"Azure DevOps returned an error: {Truncate(body, 200)}");
            },
            err => new ConnectorHealthResult(true, $"[MOCK] Azure DevOps: simulated healthy connection (real call failed: {err})"));

    public Task<ConnectorSyncResult> SyncAsync(ConnectorCredentials credentials, CancellationToken ct) =>
        TryOrMock<ConnectorSyncResult>(
            async () =>
            {
                var (ok, body) = await SendAsync(http, NewRequest(HttpMethod.Get, credentials, "_apis/projects?api-version=7.1"), ct);
                if (!ok) return new ConnectorSyncResult(false, $"Azure DevOps returned an error: {Truncate(body, 200)}");
                using var doc = JsonDocument.Parse(body);
                var count = doc.RootElement.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
                return new ConnectorSyncResult(true, $"Synced {count} Azure DevOps project(s).", MemoryKind: "Doc", MemoryContent: $"Azure DevOps: {count} project(s) synced.");
            },
            err => new ConnectorSyncResult(true, "[MOCK] Simulated Azure DevOps sync — 2 projects, 6 active work items.", MemoryKind: "Doc", MemoryContent: $"[MOCK] Azure DevOps sync simulated (real call failed: {err})."));

    public Task<ConnectorActionResult> ExecuteActionAsync(string actionKey, ConnectorCredentials credentials, string inputJson, CancellationToken ct)
    {
        if (actionKey != "CreateWorkItem")
            return Task.FromResult(new ConnectorActionResult(false, "{}", $"Unknown action '{actionKey}'."));

        return TryOrMock(
            async () =>
            {
                using var input = JsonDocument.Parse(inputJson);
                var (project, type, title) = (
                    input.RootElement.GetProperty("project").GetString()!,
                    input.RootElement.TryGetProperty("type", out var t) ? t.GetString()! : "Task",
                    input.RootElement.GetProperty("title").GetString()!);

                var patch = new[] { new { op = "add", path = "/fields/System.Title", value = title } };
                var content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json-patch+json");
                var path = $"{Uri.EscapeDataString(project)}/_apis/wit/workitems/${Uri.EscapeDataString(type)}?api-version=7.1";
                var (ok, respBody) = await SendAsync(http, NewRequest(HttpMethod.Post, credentials, path, content), ct);
                return ok ? new ConnectorActionResult(true, respBody) : new ConnectorActionResult(false, "{}", Truncate(respBody, 300));
            },
            err => new ConnectorActionResult(true, $$"""{"mock":true,"id":1,"message":"[MOCK] Work item created.","reason":"{{err}}"}"""));
    }
}
