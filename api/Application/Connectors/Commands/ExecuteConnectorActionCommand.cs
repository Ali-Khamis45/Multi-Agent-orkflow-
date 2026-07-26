using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Connectors.Common;
using AiAgentsTeam.Domain.Connectors;
using MediatR;

namespace AiAgentsTeam.Application.Connectors.Commands;

public sealed record ConnectorActionDto(bool Success, string OutputJson, string? ErrorMessage);

/// <summary>The one command that makes the platform "perform real actions" instead of
/// only advising (Phase 4's own framing) — every call is logged to
/// ConnectorActionLog regardless of outcome, since an action with real external side
/// effects needs a durable audit trail even more than a generated artifact does.</summary>
public sealed record ExecuteConnectorActionCommand(
    Guid WorkspaceId, string ConnectorKey, string ActionKey, string InputJson, Guid? CorrelationId = null) : IRequest<ConnectorActionDto>;

public sealed class ExecuteConnectorActionCommandHandler(IApplicationDbContext db, ConnectorCredentialLoader loader)
    : IRequestHandler<ExecuteConnectorActionCommand, ConnectorActionDto>
{
    public async Task<ConnectorActionDto> Handle(ExecuteConnectorActionCommand request, CancellationToken cancellationToken)
    {
        var (_, connector, credentials) = await loader.LoadAsync(request.WorkspaceId, request.ConnectorKey, cancellationToken);

        if (!connector.Actions.Any(a => a.Key == request.ActionKey))
            throw new ArgumentException($"Connector '{request.ConnectorKey}' has no action '{request.ActionKey}'.");

        var result = await connector.ExecuteActionAsync(request.ActionKey, credentials, request.InputJson, cancellationToken);

        db.ConnectorActionLogs.Add(new ConnectorActionLog(
            request.WorkspaceId, request.ConnectorKey, request.ActionKey, request.InputJson,
            result.Success, result.OutputJson, result.ErrorMessage, request.CorrelationId));
        await db.SaveChangesAsync(cancellationToken);

        return new ConnectorActionDto(result.Success, result.OutputJson, result.ErrorMessage);
    }
}
