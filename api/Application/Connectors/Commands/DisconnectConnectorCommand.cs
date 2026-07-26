using AiAgentsTeam.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Connectors.Commands;

public sealed record DisconnectConnectorCommand(Guid WorkspaceId, string ConnectorKey) : IRequest<Unit>;

public sealed class DisconnectConnectorCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DisconnectConnectorCommand, Unit>
{
    public async Task<Unit> Handle(DisconnectConnectorCommand request, CancellationToken cancellationToken)
    {
        var installation = await db.ConnectorInstallations
            .FirstOrDefaultAsync(c => c.WorkspaceId == request.WorkspaceId && c.ConnectorKey == request.ConnectorKey, cancellationToken);
        if (installation is null)
            throw new KeyNotFoundException($"Connector '{request.ConnectorKey}' is not installed for this workspace.");

        installation.Disconnect();
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
