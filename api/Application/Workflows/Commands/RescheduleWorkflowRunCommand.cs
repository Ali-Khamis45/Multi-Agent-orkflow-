using AiAgentsTeam.Application.Scheduling;
using MediatR;

namespace AiAgentsTeam.Application.Workflows.Commands;

/// <summary>
/// Explicit re-trigger of the Scheduler (§5.2) after the Supervisor (E1) dynamically
/// expands the DAG — e.g. appending Project Manager..QA once Business Analyst
/// completes. AddTaskNodeCommand/AddTaskDependencyCommand intentionally do not
/// auto-schedule on every call, since the Supervisor may add several nodes/edges
/// as one logical expansion; this command marks "the expansion is done, recompute
/// the ready set now."
/// </summary>
public sealed record RescheduleWorkflowRunCommand(Guid WorkflowRunId) : IRequest;

public sealed class RescheduleWorkflowRunCommandHandler(ISchedulerService scheduler)
    : IRequestHandler<RescheduleWorkflowRunCommand>
{
    public Task Handle(RescheduleWorkflowRunCommand request, CancellationToken cancellationToken) =>
        scheduler.ScheduleReadyNodesAsync(request.WorkflowRunId, cancellationToken);
}
