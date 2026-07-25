using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Domain.Agents;
using AiAgentsTeam.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Registry.Commands;

/// <summary>
/// Agent self-registration (ARCHITECTURE.md §4.2 step 1-2). Re-registering an
/// existing (Name, CompanyType) pair (a new deploy of the same agent) refreshes its
/// manifest rather than creating a duplicate row.
/// </summary>
public sealed record RegisterAgentCommand(
    string Name,
    CompanyType CompanyType,
    string Version,
    string Description,
    List<string> Skills,
    List<string> SupportedTasks,
    int Priority,
    List<string> RequiredContext,
    List<string> ProducedArtifacts,
    List<string> Dependencies,
    List<string> Tools,
    List<string> Permissions,
    string Endpoint,
    string? HealthCheck,
    Guid WorkspaceId) : IRequest<Guid>;

public sealed class RegisterAgentCommandHandler(IApplicationDbContext db, IEventBus eventBus)
    : IRequestHandler<RegisterAgentCommand, Guid>
{
    public async Task<Guid> Handle(RegisterAgentCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.AgentRegistrations
            .FirstOrDefaultAsync(a => a.Name == request.Name && a.CompanyType == request.CompanyType, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateManifest(
                request.Version, request.Description, request.Skills, request.SupportedTasks,
                request.Priority, request.RequiredContext, request.ProducedArtifacts,
                request.Dependencies, request.Tools, request.Permissions, request.Endpoint, request.HealthCheck);
        }
        else
        {
            existing = new AgentRegistration(
                request.Name, request.CompanyType, request.Version, request.Description, request.Skills, request.SupportedTasks,
                request.Priority, request.RequiredContext, request.ProducedArtifacts,
                request.Dependencies, request.Tools, request.Permissions, request.Endpoint, request.HealthCheck);
            db.AgentRegistrations.Add(existing);
        }

        await db.SaveChangesAsync(cancellationToken);

        await eventBus.PublishAsync(new EventEnvelope
        {
            Type = EventTypes.AgentRegistered,
            WorkspaceId = request.WorkspaceId,
            ProducedBy = request.Name,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { request.Name, request.SupportedTasks })
        }, cancellationToken);

        return existing.Id;
    }
}
