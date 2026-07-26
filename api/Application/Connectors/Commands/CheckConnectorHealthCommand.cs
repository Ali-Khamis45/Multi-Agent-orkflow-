using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Connectors.Common;
using MediatR;

namespace AiAgentsTeam.Application.Connectors.Commands;

public sealed record ConnectorHealthDto(bool Healthy, string Message);

public sealed record CheckConnectorHealthCommand(Guid WorkspaceId, string ConnectorKey) : IRequest<ConnectorHealthDto>;

public sealed class CheckConnectorHealthCommandHandler(IApplicationDbContext db, ConnectorCredentialLoader loader)
    : IRequestHandler<CheckConnectorHealthCommand, ConnectorHealthDto>
{
    public async Task<ConnectorHealthDto> Handle(CheckConnectorHealthCommand request, CancellationToken cancellationToken)
    {
        var (installation, connector, credentials) = await loader.LoadAsync(request.WorkspaceId, request.ConnectorKey, cancellationToken);

        var result = await connector.CheckHealthAsync(credentials, cancellationToken);
        installation.RecordHealth(result.Healthy, result.Message);
        await db.SaveChangesAsync(cancellationToken);

        return new ConnectorHealthDto(result.Healthy, result.Message);
    }
}
