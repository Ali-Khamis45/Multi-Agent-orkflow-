using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Domain.Intent;
using MediatR;

namespace AiAgentsTeam.Application.Intent.Commands;

public sealed record StartIntentSessionCommand(Guid WorkspaceId, string RawInput) : IRequest<Guid>;

public sealed class StartIntentSessionCommandHandler(IApplicationDbContext db, IEventBus eventBus)
    : IRequestHandler<StartIntentSessionCommand, Guid>
{
    public async Task<Guid> Handle(StartIntentSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new IntentSession(request.WorkspaceId, request.RawInput);
        db.IntentSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.IntentAnalysisStarted,
            WorkspaceId = request.WorkspaceId,
            ProducedBy = "intent-engine",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { IntentSessionId = session.Id, request.RawInput })
        }, cancellationToken);

        return session.Id;
    }
}
