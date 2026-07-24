using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Intent.Commands;

public sealed record SubmitClarificationAnswerCommand(Guid IntentSessionId, string Question, string Answer) : IRequest;

public sealed class SubmitClarificationAnswerCommandHandler(IApplicationDbContext db, IEventBus eventBus)
    : IRequestHandler<SubmitClarificationAnswerCommand>
{
    public async Task Handle(SubmitClarificationAnswerCommand request, CancellationToken cancellationToken)
    {
        var session = await db.IntentSessions
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.Id == request.IntentSessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"IntentSession {request.IntentSessionId} not found.");

        var answer = session.AddAnswer(request.Question, request.Answer);
        db.ClarificationAnswers.Add(answer);

        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.ClarificationAnswered,
            WorkspaceId = session.WorkspaceId,
            ProducedBy = "human",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { IntentSessionId = session.Id, request.Question, request.Answer })
        }, cancellationToken);
    }
}
