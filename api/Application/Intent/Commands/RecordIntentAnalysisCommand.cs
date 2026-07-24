using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Intent.Commands;

/// <summary>Called by the Intent Engine (Python) once it has classified the project and
/// extracted goals/risks. If no ambiguity was found, the session is immediately Structured.</summary>
public sealed record RecordIntentAnalysisCommand(
    Guid IntentSessionId,
    string ExtractedGoalsJson,
    string ProjectClassification,
    double ComplexityScore,
    string RiskFlagsJson,
    string AmbiguitiesJson,
    bool HasAmbiguity) : IRequest;

public sealed class RecordIntentAnalysisCommandHandler(IApplicationDbContext db, IEventBus eventBus)
    : IRequestHandler<RecordIntentAnalysisCommand>
{
    public async Task Handle(RecordIntentAnalysisCommand request, CancellationToken cancellationToken)
    {
        var session = await db.IntentSessions.FirstOrDefaultAsync(s => s.Id == request.IntentSessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"IntentSession {request.IntentSessionId} not found.");

        session.RecordAnalysis(
            request.ExtractedGoalsJson, request.ProjectClassification, request.ComplexityScore,
            request.RiskFlagsJson, request.AmbiguitiesJson, request.HasAmbiguity);

        await db.SaveChangesAsync(cancellationToken);

        var eventType = request.HasAmbiguity ? EventTypes.ClarificationRequested : EventTypes.ProjectClassified;
        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = eventType,
            WorkspaceId = session.WorkspaceId,
            ProducedBy = "intent-engine",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                IntentSessionId = session.Id,
                request.ProjectClassification,
                request.AmbiguitiesJson
            })
        }, cancellationToken);
    }
}
