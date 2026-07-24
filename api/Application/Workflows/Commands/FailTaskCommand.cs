using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Application.Scheduling;
using AiAgentsTeam.Domain.Failures;
using AiAgentsTeam.Domain.Workflow;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAgentsTeam.Application.Workflows.Commands;

/// <summary>
/// Reaction to a <c>TaskFailed</c> event (ARCHITECTURE.md §10 Self-Healing). Retries
/// up to a configurable policy (default 2 attempts, see SchedulerOptions) by
/// resetting the node to Pending so the next scheduling pass re-resolves a
/// candidate agent — possibly a different one, if the original is now unavailable
/// or busy. Exhausted retries fail the whole run for Phase 1; branching around
/// non-critical failures is deferred (build order §24).
/// </summary>
public sealed record FailTaskCommand(Guid TaskNodeId, string? ReasonJson) : IRequest;

public sealed class FailTaskCommandHandler(
    IApplicationDbContext db,
    IEventBus eventBus,
    ISchedulerService scheduler,
    IOptions<SchedulerOptions> options) : IRequestHandler<FailTaskCommand>
{
    public async Task Handle(FailTaskCommand request, CancellationToken cancellationToken)
    {
        var workflowRunId = await db.TaskNodes
            .Where(n => n.Id == request.TaskNodeId)
            .Select(n => n.WorkflowRunId)
            .FirstOrDefaultAsync(cancellationToken);

        var run = await db.WorkflowRuns
            .Include(r => r.Nodes)
            .Include(r => r.Edges)
            .FirstOrDefaultAsync(r => r.Id == workflowRunId, cancellationToken)
            ?? throw new KeyNotFoundException($"WorkflowRun for task {request.TaskNodeId} not found.");

        var node = run.Nodes.First(n => n.Id == request.TaskNodeId);

        // Idempotency (Phase 1.5 §3): Redis Streams delivers at-least-once. Only a
        // node still genuinely in-flight (Dispatched/Running) can be legitimately
        // failed; any other status means a previous delivery of this same event
        // (or a newer one) already resolved it — reprocessing would double-decrement
        // the agent's in-flight count, double-retry, or double-fail the run.
        if (node.Status is not (TaskNodeStatus.Dispatched or TaskNodeStatus.Running))
            return;

        if (node.AssignedAgentName is not null)
        {
            var agent = await db.AgentRegistrations
                .FirstOrDefaultAsync(a => a.Name == node.AssignedAgentName, cancellationToken);
            agent?.DecrementInFlight();
        }

        node.Fail(request.ReasonJson);

        // Structured Error Model (Phase 1.5 §4): every failure is recorded with a
        // fixed, queryable shape — not just a free-text exception string. The AI
        // Runtime publishes this shape in ReasonJson (see StructuredFailure in
        // app/reasoning/failures.py); a legacy/unstructured payload degrades
        // gracefully into an "Unknown" category rather than throwing here.
        var structured = ParseStructuredFailure(request.ReasonJson);
        db.ExecutionFailures.Add(new ExecutionFailure(
            node.Id, run.Id, run.CorrelationId, node.AssignedAgentName ?? "unknown",
            structured.Category, structured.Severity, structured.Recoverable, structured.Retryable,
            structured.Message, structured.Stack, structured.SuggestedAction));

        if (node.AttemptCount < options.Value.MaxTaskAttempts && structured.Retryable)
        {
            node.ResetForRetry();
            await scheduler.ScheduleReadyNodesAsync(run.Id, cancellationToken);
            return;
        }

        run.Fail();
        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.WorkflowRunFailed,
            WorkspaceId = run.WorkspaceId,
            WorkflowRunId = run.Id,
            TaskId = node.Id,
            CorrelationId = run.CorrelationId,
            ProducedBy = "scheduler",
            PayloadJson = JsonSerializer.Serialize(new { run.Id, FailedNodeId = node.Id, node.Name })
        }, cancellationToken);
    }

    private sealed record StructuredFailureShape(
        FailureCategory Category, FailureSeverity Severity, bool Recoverable, bool Retryable,
        string Message, string? Stack, string? SuggestedAction);

    private static StructuredFailureShape ParseStructuredFailure(string? reasonJson)
    {
        if (reasonJson is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(reasonJson);
                var r = doc.RootElement;
                if (r.TryGetProperty("category", out _))
                {
                    return new StructuredFailureShape(
                        Enum.TryParse<FailureCategory>(r.GetProperty("category").GetString(), true, out var cat) ? cat : FailureCategory.Unknown,
                        Enum.TryParse<FailureSeverity>(r.GetProperty("severity").GetString(), true, out var sev) ? sev : FailureSeverity.Medium,
                        r.TryGetProperty("recoverable", out var rec) && rec.GetBoolean(),
                        r.TryGetProperty("retryable", out var retry) && retry.GetBoolean(),
                        r.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "",
                        r.TryGetProperty("stack", out var stack) ? stack.GetString() : null,
                        r.TryGetProperty("suggestedAction", out var action) ? action.GetString() : null);
                }
            }
            catch (JsonException)
            {
                // Falls through to the Unknown-category default below.
            }
        }

        return new StructuredFailureShape(
            FailureCategory.Unknown, FailureSeverity.Medium, false, true, reasonJson ?? "(no reason provided)", null, null);
    }
}
