using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Workflow;
using MediatR;

namespace AiAgentsTeam.Application.Workflows.Commands;

public sealed record CreateWorkflowRunCommand(Guid WorkspaceId, string Goal) : IRequest<Guid>;

public sealed class CreateWorkflowRunCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateWorkflowRunCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkflowRunCommand request, CancellationToken cancellationToken)
    {
        var run = new WorkflowRun(request.WorkspaceId, request.Goal);
        db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        return run.Id;
    }
}
