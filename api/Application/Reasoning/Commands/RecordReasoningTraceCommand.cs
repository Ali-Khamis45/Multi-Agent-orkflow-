using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Domain.Reasoning;
using MediatR;

namespace AiAgentsTeam.Application.Reasoning.Commands;

/// <summary>
/// Records one stage of the Reasoning Engine pipeline (ARCHITECTURE_EXTENSION.md
/// §E6). Called async/fire-and-forget by the AI Runtime after every stage — never
/// blocks the agent's actual execution.
/// </summary>
public sealed record RecordReasoningTraceCommand(
    Guid TaskNodeId, string Agent, ReasoningStage Stage, string? InputJson, string? OutputJson, long DurationMs)
    : IRequest<Guid>;

public sealed class RecordReasoningTraceCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RecordReasoningTraceCommand, Guid>
{
    public async Task<Guid> Handle(RecordReasoningTraceCommand request, CancellationToken cancellationToken)
    {
        var trace = new ReasoningTrace(
            request.TaskNodeId, request.Agent, request.Stage, request.InputJson, request.OutputJson, request.DurationMs);

        db.ReasoningTraces.Add(trace);
        await db.SaveChangesAsync(cancellationToken);
        return trace.Id;
    }
}
