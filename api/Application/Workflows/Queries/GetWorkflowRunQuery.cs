using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workflows.Queries;

public sealed record GetWorkflowRunQuery(Guid WorkflowRunId) : IRequest<WorkflowRunDto?>;

public sealed record TaskNodeDto(
    Guid Id, string Name, string TaskType, string Status, string? AssignedAgentName,
    double? Confidence, string? RiskLevel, int AttemptCount);

public sealed record TaskEdgeDto(Guid PredecessorNodeId, Guid SuccessorNodeId);

public sealed record WorkflowRunDto(
    Guid Id, Guid WorkspaceId, Guid CorrelationId, string Goal, string Status,
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

        if (run is null) return null;

        return new WorkflowRunDto(
            run.Id, run.WorkspaceId, run.CorrelationId, run.Goal, run.Status.ToString(),
            run.Nodes.Select(n => new TaskNodeDto(
                n.Id, n.Name, n.TaskType, n.Status.ToString(), n.AssignedAgentName,
                n.Confidence, n.RiskLevel, n.AttemptCount)).ToList(),
            run.Edges.Select(e => new TaskEdgeDto(e.PredecessorNodeId, e.SuccessorNodeId)).ToList());
    }
}
