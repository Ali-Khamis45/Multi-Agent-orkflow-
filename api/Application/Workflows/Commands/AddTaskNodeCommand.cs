using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Workflows.Commands;

/// <summary>
/// Adds one DAG node to an existing WorkflowRun. Called by the Supervisor (E1) while
/// it dynamically builds the execution graph — the Scheduler has no opinion on shape.
/// </summary>
public sealed record AddTaskNodeCommand(
    Guid WorkflowRunId,
    string Name,
    string TaskType,
    string InputsJson,
    bool RequiresApproval = false) : IRequest<Guid>;

public sealed class AddTaskNodeCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AddTaskNodeCommand, Guid>
{
    public async Task<Guid> Handle(AddTaskNodeCommand request, CancellationToken cancellationToken)
    {
        var run = await db.WorkflowRuns
            .Include(r => r.Nodes)
            .FirstOrDefaultAsync(r => r.Id == request.WorkflowRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowRun {request.WorkflowRunId} not found.");

        // Idempotency (Phase 1.5 §3): a retried "expand the DAG" call from the
        // Supervisor (e.g. after a network blip mid-expansion) must not create a
        // duplicate node — node names are unique per run by convention.
        var existing = run.Nodes.FirstOrDefault(n => n.Name == request.Name);
        if (existing is not null)
            return existing.Id;

        var node = run.AddNode(request.Name, request.TaskType, request.InputsJson, request.RequiresApproval);

        // Explicitly track the new child — EF Core's snapshot change detection for a
        // field-backed IReadOnlyCollection navigation does not reliably infer Added
        // state on its own; an explicit Add makes the entity state unambiguous.
        db.TaskNodes.Add(node);

        await db.SaveChangesAsync(cancellationToken);
        return node.Id;
    }
}
