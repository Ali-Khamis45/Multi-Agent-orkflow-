using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workflows.Commands;

public sealed record AddTaskDependencyCommand(Guid WorkflowRunId, Guid PredecessorNodeId, Guid SuccessorNodeId)
    : IRequest<Guid>;

public sealed class AddTaskDependencyCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AddTaskDependencyCommand, Guid>
{
    public async Task<Guid> Handle(AddTaskDependencyCommand request, CancellationToken cancellationToken)
    {
        var run = await db.WorkflowRuns
            .Include(r => r.Nodes)
            .Include(r => r.Edges)
            .FirstOrDefaultAsync(r => r.Id == request.WorkflowRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowRun {request.WorkflowRunId} not found.");

        var predecessor = run.Nodes.First(n => n.Id == request.PredecessorNodeId);
        var successor = run.Nodes.First(n => n.Id == request.SuccessorNodeId);

        var edge = run.AddDependency(predecessor, successor);
        db.TaskEdges.Add(edge);

        await db.SaveChangesAsync(cancellationToken);
        return edge.Id;
    }
}
