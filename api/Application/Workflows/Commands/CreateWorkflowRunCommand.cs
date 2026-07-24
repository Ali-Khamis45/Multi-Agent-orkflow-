using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Workflow;
using MediatR;

namespace AiAgentsTeam.Application.Workflows.Commands;

/// <summary>
/// CorrelationId (Phase 1.5 §2) is normally minted by the caller (the Supervisor,
/// at the very start of an execution) so it can be threaded through every
/// subsequent call this same execution makes; if omitted, a fresh one is generated.
/// </summary>
public sealed record CreateWorkflowRunCommand(Guid WorkspaceId, string Goal, Guid? CorrelationId = null) : IRequest<Guid>;

public sealed class CreateWorkflowRunCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateWorkflowRunCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkflowRunCommand request, CancellationToken cancellationToken)
    {
        var run = new WorkflowRun(request.WorkspaceId, request.Goal, correlationId: request.CorrelationId);
        db.WorkflowRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        return run.Id;
    }
}
