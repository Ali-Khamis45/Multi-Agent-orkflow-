using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Workflow;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workflows.Queries;

public sealed record GetWorkflowRunQuery(Guid WorkflowRunId) : IRequest<WorkflowRunDto?>;

public sealed record TaskNodeDto(
    Guid Id, string Name, string TaskType, string Status, string? AssignedAgentName,
    double? Confidence, string? RiskLevel, int AttemptCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record TaskEdgeDto(Guid PredecessorNodeId, Guid SuccessorNodeId);

public sealed record WorkflowRunDto(
    Guid Id, Guid WorkspaceId, Guid CorrelationId, string Goal, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyCollection<TaskNodeDto> Nodes, IReadOnlyCollection<TaskEdgeDto> Edges);

public sealed class GetWorkflowRunQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkflowRunQuery, WorkflowRunDto?>
{
    public async Task<WorkflowRunDto?> Handle(GetWorkflowRunQuery request, CancellationToken cancellationToken)
    {
        var run = await db.WorkflowRuns
            .Include(r => r.Nodes)
            .Include(r => r.Edges)
            .FirstOrDefaultAsync(r => r.Id == request.WorkflowRunId, cancellationToken);

        return run is null ? null : ToDto(run);
    }

    internal static WorkflowRunDto ToDto(WorkflowRun run) => new(
        run.Id, run.WorkspaceId, run.CorrelationId, run.Goal, run.Status.ToString(),
        run.CreatedAt, run.UpdatedAt,
        run.Nodes.Select(n => new TaskNodeDto(
            n.Id, n.Name, n.TaskType, n.Status.ToString(), n.AssignedAgentName,
            n.Confidence, n.RiskLevel, n.AttemptCount, n.CreatedAt, n.UpdatedAt)).ToList(),
        run.Edges.Select(e => new TaskEdgeDto(e.PredecessorNodeId, e.SuccessorNodeId)).ToList());
}

/// <summary>Lists WorkflowRuns for the Workflow Runs page — optionally filtered by
/// workspace/status, newest first.</summary>
public sealed record GetWorkflowRunsQuery(Guid? WorkspaceId = null, string? Status = null, int Limit = 50)
    : IRequest<IReadOnlyCollection<WorkflowRunDto>>;

public sealed class GetWorkflowRunsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetWorkflowRunsQuery, IReadOnlyCollection<WorkflowRunDto>>
{
    public async Task<IReadOnlyCollection<WorkflowRunDto>> Handle(GetWorkflowRunsQuery request, CancellationToken cancellationToken)
    {
        var query = db.WorkflowRuns.Include(r => r.Nodes).Include(r => r.Edges).AsQueryable();

        if (request.WorkspaceId is { } workspaceId)
            query = query.Where(r => r.WorkspaceId == workspaceId);

        if (request.Status is { } status && Enum.TryParse<WorkflowRunStatus>(status, true, out var parsed))
            query = query.Where(r => r.Status == parsed);

        var runs = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return runs.Select(GetWorkflowRunQueryHandler.ToDto).ToList();
    }
}
