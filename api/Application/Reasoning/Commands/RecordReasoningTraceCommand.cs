using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Reasoning;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Reasoning.Commands;

/// <summary>
/// Records one stage of the Reasoning Engine pipeline — the unified execution
/// telemetry record (Phase 1.5 §1 Observability, §E6). Called async/fire-and-forget
/// by the AI Runtime after every stage — never blocks the agent's actual execution.
/// WorkflowRunId/CorrelationId are derived server-side from the TaskNode (Phase 1.5
/// §2) rather than trusted from the caller.
/// </summary>
public sealed record RecordReasoningTraceCommand(
    Guid TaskNodeId,
    string Agent,
    ReasoningStage Stage,
    DateTimeOffset StartedAt,
    long DurationMs,
    string? InputJson,
    string? OutputJson,
    int? Tokens,
    double? Confidence,
    string? ModelUsed,
    int RetryCount,
    int MemoryReads,
    int MemoryWrites,
    int ToolCalls,
    double? CostEstimate,
    string? ErrorMessage) : IRequest<Guid>;

public sealed class RecordReasoningTraceCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RecordReasoningTraceCommand, Guid>
{
    public async Task<Guid> Handle(RecordReasoningTraceCommand request, CancellationToken cancellationToken)
    {
        var node = await db.TaskNodes
            .Where(n => n.Id == request.TaskNodeId)
            .Select(n => new { n.WorkflowRunId, n.CorrelationId })
            .FirstOrDefaultAsync(cancellationToken);

        var trace = new ReasoningTrace(
            request.TaskNodeId, node?.WorkflowRunId ?? Guid.Empty, node?.CorrelationId ?? Guid.Empty,
            request.Agent, request.Stage,
            request.StartedAt, request.DurationMs, request.InputJson, request.OutputJson,
            request.Tokens, request.Confidence, request.ModelUsed, request.RetryCount,
            request.MemoryReads, request.MemoryWrites, request.ToolCalls, request.CostEstimate, request.ErrorMessage);

        db.ReasoningTraces.Add(trace);
        await db.SaveChangesAsync(cancellationToken);
        return trace.Id;
    }
}
