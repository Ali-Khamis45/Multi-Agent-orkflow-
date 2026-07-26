using AiAgentsTeam.Application.Connectors.Abstractions;
using MediatR;

namespace AiAgentsTeam.Application.Connectors.Commands;

/// <summary>The OAuth2 redirect-callback handler. Verifies the signed `state` param
/// (proves this callback belongs to a workspace that actually initiated this exact
/// connector's authorize flow — see IConnectorOAuthStateSigner), exchanges the code for
/// a token via the one generic OAuth2 exchange (IOAuth2TokenExchanger), then reuses
/// InstallConnectorCommand to persist it exactly like an API-key install would.</summary>
public sealed record CompleteConnectorOAuthCommand(string ConnectorKey, string Code, string State) : IRequest<Unit>;

public sealed class CompleteConnectorOAuthCommandHandler(
    IConnectorRegistry registry, IConnectorOAuthStateSigner stateSigner, IOAuth2TokenExchanger exchanger,
    IConnectorConfig config, ISender sender)
    : IRequestHandler<CompleteConnectorOAuthCommand, Unit>
{
    public async Task<Unit> Handle(CompleteConnectorOAuthCommand request, CancellationToken cancellationToken)
    {
        var verified = stateSigner.Verify(request.State)
            ?? throw new UnauthorizedAccessException("OAuth state is invalid, expired, or does not match this connector.");
        if (verified.ConnectorKey != request.ConnectorKey)
            throw new UnauthorizedAccessException("OAuth state does not match this connector.");

        var connector = registry.Require(request.ConnectorKey);
        if (connector.OAuth is null)
            throw new InvalidOperationException($"Connector '{request.ConnectorKey}' does not use OAuth2.");

        var redirectBaseUrl = config.Require("Connectors:RedirectBaseUrl");
        var redirectUri = $"{redirectBaseUrl.TrimEnd('/')}/api/connectors/{request.ConnectorKey}/oauth/callback";

        var tokenFields = await exchanger.ExchangeCodeAsync(connector.OAuth, request.Code, redirectUri, cancellationToken);

        await sender.Send(new InstallConnectorCommand(verified.WorkspaceId, request.ConnectorKey, tokenFields), cancellationToken);
        return Unit.Value;
    }
}
