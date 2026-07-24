using AiAgentsTeam.Application.Intent.Commands;
using AiAgentsTeam.Application.Intent.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Intent Engine endpoints (ARCHITECTURE_EXTENSION.md §E2).</summary>
[ApiController]
[Route("api/intent")]
public sealed class IntentController(ISender sender) : ControllerBase
{
    public sealed record StartSessionRequest(Guid WorkspaceId, string RawInput);

    [HttpPost("sessions")]
    public async Task<ActionResult<Guid>> Start(StartSessionRequest request, CancellationToken ct) =>
        Ok(await sender.Send(new StartIntentSessionCommand(request.WorkspaceId, request.RawInput), ct));

    [HttpPost("sessions/{id:guid}/analysis")]
    public async Task<IActionResult> RecordAnalysis(Guid id, RecordIntentAnalysisBody body, CancellationToken ct)
    {
        await sender.Send(new RecordIntentAnalysisCommand(
            id, body.ExtractedGoalsJson, body.ProjectClassification, body.ComplexityScore,
            body.RiskFlagsJson, body.AmbiguitiesJson, body.HasAmbiguity), ct);
        return NoContent();
    }

    public sealed record RecordIntentAnalysisBody(
        string ExtractedGoalsJson, string ProjectClassification, double ComplexityScore,
        string RiskFlagsJson, string AmbiguitiesJson, bool HasAmbiguity);

    public sealed record SubmitAnswerRequest(string Question, string Answer);

    [HttpPost("sessions/{id:guid}/answers")]
    public async Task<IActionResult> SubmitAnswer(Guid id, SubmitAnswerRequest request, CancellationToken ct)
    {
        await sender.Send(new SubmitClarificationAnswerCommand(id, request.Question, request.Answer), ct);
        return NoContent();
    }

    public sealed record MarkStructuredRequest(Guid StructuredRequirementsArtifactId);

    [HttpPost("sessions/{id:guid}/structure")]
    public async Task<IActionResult> MarkStructured(Guid id, MarkStructuredRequest request, CancellationToken ct)
    {
        await sender.Send(new MarkIntentStructuredCommand(id, request.StructuredRequirementsArtifactId), ct);
        return NoContent();
    }

    [HttpGet("sessions/{id:guid}")]
    public async Task<ActionResult<IntentSessionDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await sender.Send(new GetIntentSessionQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
