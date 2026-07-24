using AiAgentsTeam.Application.Artifacts.Commands;
using AiAgentsTeam.Application.Artifacts.Queries;
using AiAgentsTeam.Domain.Artifacts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Artifact Manager endpoints (ARCHITECTURE.md §14.1) — versioned, never overwritten.</summary>
[ApiController]
[Route("api/artifacts")]
public sealed class ArtifactsController(ISender sender) : ControllerBase
{
    public sealed record CreateArtifactRequest(
        Guid WorkspaceId, string Name, ArtifactType Type, string OwnerAgent, string? Content,
        Guid? WorkflowRunId, Guid? TaskNodeId, Guid? PreviousVersionId,
        Guid? CorrelationId = null, string? IdempotencyKey = null);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateArtifactRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new CreateArtifactCommand(
            request.WorkspaceId, request.Name, request.Type, request.OwnerAgent, request.Content,
            request.WorkflowRunId, request.TaskNodeId, request.PreviousVersionId,
            request.CorrelationId, request.IdempotencyKey), ct);
        return Ok(id);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArtifactDto>> Get(Guid id, CancellationToken ct)
    {
        var dto = await sender.Send(new GetArtifactQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<IReadOnlyCollection<ArtifactDto>>> GetVersions(Guid id, CancellationToken ct) =>
        Ok(await sender.Send(new GetArtifactVersionsQuery(id), ct));

    [HttpGet("by-name")]
    public async Task<ActionResult<ArtifactDto>> GetByName(
        [FromQuery] Guid workflowRunId, [FromQuery] string name, CancellationToken ct)
    {
        var dto = await sender.Send(new GetLatestArtifactByNameQuery(workflowRunId, name), ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
