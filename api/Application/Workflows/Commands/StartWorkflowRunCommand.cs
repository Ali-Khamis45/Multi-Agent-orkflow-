using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Application.Scheduling;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workflows.Commands;

public sealed record StartWorkflowRunCommand(Guid WorkflowRunId) : IRequest;

public sealed class StartWorkflowRunCommandHandler(
    IApplicationDbContext db,
    IEventBus eventBus,
    ISchedulerService scheduler) : IRequestHandler<StartWorkflowRunCommand>
{
    public async Task Handle(StartWorkflowRunCommand request, CancellationToken cancellationToken)
    {
        var run = await db.WorkflowRuns
            .FirstOrDefaultAsync(r => r.Id == request.WorkflowRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowRun {request.WorkflowRunId} not found.");

        run.Start();
        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.WorkflowRunStarted,
            WorkspaceId = run.WorkspaceId,
            WorkflowRunId = run.Id,
            CorrelationId = run.CorrelationId,
            ProducedBy = "supervisor",
            PayloadJson = JsonSerializer.Serialize(new { run.Id, run.Goal })
        }, cancellationToken);

        // §5.2 step 1: compute the initial ready set now that the run is Running.
        await scheduler.ScheduleReadyNodesAsync(run.Id, cancellationToken);
    }
}
