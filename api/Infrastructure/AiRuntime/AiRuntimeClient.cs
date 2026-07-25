using System.Net.Http.Json;
using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;

namespace AiAgentsTeam.Infrastructure.AiRuntime;

public sealed class AiRuntimeClient(HttpClient http) : IAiRuntimeClient
{
    public async Task<Guid> SubmitIntakeAsync(string rawInput, Guid? workspaceId, string companyType, CancellationToken cancellationToken)
    {
        var response = await http.PostAsJsonAsync(
            "/intake",
            new { raw_input = rawInput, workspace_id = workspaceId, company_type = companyType },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<IntakeResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("AI Runtime returned an empty /intake response.");
        return result.workflow_run_id;
    }

    public async Task<string> GetPromptsJsonAsync(CancellationToken cancellationToken)
    {
        var response = await http.GetAsync("/prompts", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private sealed record IntakeResponse(Guid workflow_run_id);
}
