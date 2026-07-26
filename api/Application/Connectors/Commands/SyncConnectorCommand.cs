using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Connectors.Common;
using AiAgentsTeam.Application.Founders.Commands;
using AiAgentsTeam.Application.Memory.Commands;
using AiAgentsTeam.Domain.Memory;
using MediatR;

namespace AiAgentsTeam.Application.Connectors.Commands;

public sealed record ConnectorSyncDto(bool Success, string Summary);

/// <summary>
/// "Memory Synchronization" (Phase 4): applies whatever a connector's sync found to the
/// workspace's actual memory system — CompanyProfile for Founder connectors (via the
/// same PatchCompanyProfileSectionCommand every Smart Agent already uses, Phase 3),
/// Project-layer Memory for Software connectors. This handler only branches on which
/// *fields the result populated*, never on which connector produced it — it has no idea
/// whether it's looking at Stripe or GitHub output, which is what keeps connector-
/// specific logic out of the core sync path.
/// </summary>
public sealed record SyncConnectorCommand(Guid WorkspaceId, string ConnectorKey) : IRequest<ConnectorSyncDto>;

public sealed class SyncConnectorCommandHandler(IApplicationDbContext db, ConnectorCredentialLoader loader, ISender sender)
    : IRequestHandler<SyncConnectorCommand, ConnectorSyncDto>
{
    public async Task<ConnectorSyncDto> Handle(SyncConnectorCommand request, CancellationToken cancellationToken)
    {
        var (installation, connector, credentials) = await loader.LoadAsync(request.WorkspaceId, request.ConnectorKey, cancellationToken);

        var result = await connector.SyncAsync(credentials, cancellationToken);
        installation.RecordSync(result.Success, result.Summary);
        await db.SaveChangesAsync(cancellationToken);

        if (result.Success && result.CompanyProfileSection is not null && result.CompanyProfilePatch is not null)
        {
            await sender.Send(
                new PatchCompanyProfileSectionCommand(request.WorkspaceId, result.CompanyProfileSection, JsonSerializer.Serialize(result.CompanyProfilePatch)),
                cancellationToken);
        }

        if (result.Success && result.MemoryKind is not null && result.MemoryContent is not null)
        {
            await sender.Send(
                new WriteMemoryItemCommand(
                    request.WorkspaceId, MemoryLayer.Project, request.WorkspaceId, Enum.Parse<MemoryKind>(result.MemoryKind),
                    result.MemoryContent, SourceArtifactId: null, TtlAt: null, CorrelationId: null),
                cancellationToken);
        }

        return new ConnectorSyncDto(result.Success, result.Summary);
    }
}
