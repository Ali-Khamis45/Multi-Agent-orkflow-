using AiAgentsTeam.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>
/// Prompt Registry (Phase 1.5 §8) read-through. The registry itself is a
/// file-based catalog owned by the AI Runtime (app/prompts/registry.json);
/// this proxies it so the dashboard (Phase 1.6) still only ever talks to
/// this API, never the AI Runtime directly.
/// </summary>
[ApiController]
[Route("api/prompts")]
public sealed class PromptsController(IAiRuntimeClient aiRuntime) : ControllerBase
{
    [HttpGet]
    public async Task<ContentResult> GetAll(CancellationToken ct)
    {
        var json = await aiRuntime.GetPromptsJsonAsync(ct);
        return new ContentResult { Content = json, ContentType = "application/json", StatusCode = 200 };
    }
}
