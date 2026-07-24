using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Intent.Queries;

public sealed record IntentSessionDto(
    Guid Id, Guid WorkspaceId, Guid CorrelationId, string RawInput, string Status,
    string? ExtractedGoalsJson, string? ProjectClassification, double? ComplexityScore,
    string? RiskFlagsJson, string? AmbiguitiesJson, Guid? StructuredRequirementsArtifactId);

public sealed record GetIntentSessionQuery(Guid IntentSessionId) : IRequest<IntentSessionDto?>;

public sealed class GetIntentSessionQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetIntentSessionQuery, IntentSessionDto?>
{
    public async Task<IntentSessionDto?> Handle(GetIntentSessionQuery request, CancellationToken cancellationToken)
    {
        var s = await db.IntentSessions.FirstOrDefaultAsync(x => x.Id == request.IntentSessionId, cancellationToken);
        return s is null ? null : new IntentSessionDto(
            s.Id, s.WorkspaceId, s.CorrelationId, s.RawInput, s.Status.ToString(),
            s.ExtractedGoalsJson, s.ProjectClassification, s.ComplexityScore,
            s.RiskFlagsJson, s.AmbiguitiesJson, s.StructuredRequirementsArtifactId);
    }
}
