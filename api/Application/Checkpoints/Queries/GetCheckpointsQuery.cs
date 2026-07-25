using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Checkpoints.Queries;

public sealed record CheckpointDto(Guid Id, Guid WorkflowRunId, string Label, string SnapshotJson, DateTimeOffset CreatedAt);

/// <summary>
/// Execution Snapshots (Phase 1.5 §5) — every scheduling-pass checkpoint for a
/// run, oldest first. This is the raw material for Mission Control's Execution
/// Playback: each snapshot is a full point-in-time copy of every node's status,
/// agent, confidence, and risk, so the frontend can scrub through real historical
/// DAG state rather than animating between only the current and final state.
/// </summary>
public sealed record GetCheckpointsQuery(Guid WorkflowRunId) : IRequest<IReadOnlyCollection<CheckpointDto>>;

public sealed class GetCheckpointsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCheckpointsQuery, IReadOnlyCollection<CheckpointDto>>
{
    public async Task<IReadOnlyCollection<CheckpointDto>> Handle(GetCheckpointsQuery request, CancellationToken cancellationToken)
    {
        return await db.Checkpoints
            .Where(c => c.WorkflowRunId == request.WorkflowRunId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CheckpointDto(c.Id, c.WorkflowRunId, c.Label, c.SnapshotJson, c.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
