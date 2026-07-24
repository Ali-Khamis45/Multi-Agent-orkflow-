using AiAgentsTeam.Application.Memory.Commands;
using AiAgentsTeam.Application.Memory.Queries;
using AiAgentsTeam.Domain.Memory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentsTeam.Api.Controllers;

/// <summary>Multi-Layer Memory endpoints (ARCHITECTURE_EXTENSION.md §E5) — Phase 1 scope: Working/Conversation/Project.</summary>
[ApiController]
[Route("api/memory")]
public sealed class MemoryController(ISender sender) : ControllerBase
{
    public sealed record WriteMemoryRequest(
        Guid WorkspaceId, MemoryLayer Layer, Guid ScopeRef, MemoryKind Kind, string Content,
        Guid? SourceArtifactId, DateTimeOffset? TtlAt, Guid? CorrelationId = null);

    [HttpPost]
    public async Task<ActionResult<Guid>> Write(WriteMemoryRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new WriteMemoryItemCommand(
            request.WorkspaceId, request.Layer, request.ScopeRef, request.Kind, request.Content,
            request.SourceArtifactId, request.TtlAt, request.CorrelationId), ct);
        return Ok(id);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MemoryItemDto>>> Query(
        [FromQuery] Guid workspaceId, [FromQuery] MemoryLayer layer, [FromQuery] Guid scopeRef,
        [FromQuery] int limit, CancellationToken ct) =>
        Ok(await sender.Send(new QueryMemoryQuery(workspaceId, layer, scopeRef, limit == 0 ? 20 : limit), ct));
}
