using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Intent.Commands;

/// <summary>Called once the StructuredRequirements artifact has been written, completing the intake phase.</summary>
public sealed record MarkIntentStructuredCommand(Guid IntentSessionId, Guid StructuredRequirementsArtifactId) : IRequest;

public sealed class MarkIntentStructuredCommandHandler(IApplicationDbContext db, IEventBus eventBus)
    : IRequestHandler<MarkIntentStructuredCommand>
{
    public async Task Handle(MarkIntentStructuredCommand request, CancellationToken cancellationToken)
    {
        var session = await db.IntentSessions.FirstOrDefaultAsync(s => s.Id == request.IntentSessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"IntentSession {request.IntentSessionId} not found.");

        session.MarkStructured(request.StructuredRequirementsArtifactId);
        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.RequirementsStructured,
            WorkspaceId = session.WorkspaceId,
            ProducedBy = "intent-engine",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                IntentSessionId = session.Id,
                request.StructuredRequirementsArtifactId
            })
        }, cancellationToken);
    }
}
