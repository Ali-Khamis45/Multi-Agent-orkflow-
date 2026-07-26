using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Connectors.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Connectors.Queries;

public sealed record InstalledConnectorDto(
    string ConnectorKey,
    string DisplayName,
    string Status,
    DateTimeOffset? LastHealthCheckAt,
    bool? LastHealthOk,
    string? LastHealthMessage,
    DateTimeOffset? LastSyncedAt,
    bool? LastSyncOk,
    string? LastSyncMessage);

public sealed record GetInstalledConnectorsQuery(Guid WorkspaceId) : IRequest<IReadOnlyList<InstalledConnectorDto>>;

public sealed class GetInstalledConnectorsQueryHandler(IApplicationDbContext db, IConnectorRegistry registry)
    : IRequestHandler<GetInstalledConnectorsQuery, IReadOnlyList<InstalledConnectorDto>>
{
    public async Task<IReadOnlyList<InstalledConnectorDto>> Handle(GetInstalledConnectorsQuery request, CancellationToken cancellationToken)
    {
        var installations = await db.ConnectorInstallations
            .Where(c => c.WorkspaceId == request.WorkspaceId)
            .OrderBy(c => c.ConnectorKey)
            .ToListAsync(cancellationToken);

        return installations
            .Select(i => new InstalledConnectorDto(
                i.ConnectorKey,
                registry.Find(i.ConnectorKey)?.DisplayName ?? i.ConnectorKey,
                i.Status.ToString(),
                i.LastHealthCheckAt, i.LastHealthOk, i.LastHealthMessage,
                i.LastSyncedAt, i.LastSyncOk, i.LastSyncMessage))
            .ToList();
    }
}
