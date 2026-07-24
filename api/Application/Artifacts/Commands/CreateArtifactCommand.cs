using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Artifacts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Artifacts.Commands;

/// <summary>
/// Creates a new artifact, or a new version of one if <see cref="PreviousVersionId"/>
/// is supplied — nothing is ever overwritten (ARCHITECTURE.md §14.1).
/// </summary>
public sealed record CreateArtifactCommand(
    Guid WorkspaceId,
    string Name,
    ArtifactType Type,
    string OwnerAgent,
    string? Content,
    Guid? WorkflowRunId,
    Guid? TaskNodeId,
    Guid? PreviousVersionId,
    Guid? CorrelationId = null,
    string? IdempotencyKey = null) : IRequest<Guid>;

public sealed class CreateArtifactCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateArtifactCommand, Guid>
{
    public async Task<Guid> Handle(CreateArtifactCommand request, CancellationToken cancellationToken)
    {
        // Idempotency (Phase 1.5 §3): if this exact key already produced an
        // artifact for this run, return it rather than creating a duplicate version.
        if (request is { WorkflowRunId: { } runId, IdempotencyKey: { } key })
        {
            var existing = await db.Artifacts.FirstOrDefaultAsync(
                a => a.WorkflowRunId == runId && a.IdempotencyKey == key, cancellationToken);
            if (existing is not null)
                return existing.Id;
        }

        Artifact artifact;

        if (request.PreviousVersionId is { } previousId)
        {
            var previous = await db.Artifacts.FirstOrDefaultAsync(a => a.Id == previousId, cancellationToken)
                ?? throw new KeyNotFoundException($"Artifact {previousId} not found.");
            artifact = previous.CreateNewVersion(request.OwnerAgent, request.Content, request.IdempotencyKey);
        }
        else
        {
            artifact = new Artifact(
                request.WorkspaceId, request.Name, request.Type, request.OwnerAgent, request.Content,
                request.WorkflowRunId, request.TaskNodeId, correlationId: request.CorrelationId,
                idempotencyKey: request.IdempotencyKey);
        }

        db.Artifacts.Add(artifact);
        await db.SaveChangesAsync(cancellationToken);
        return artifact.Id;
    }
}
