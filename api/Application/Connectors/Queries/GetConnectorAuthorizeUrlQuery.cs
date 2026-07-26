using AiAgentsTeam.Application.Connectors.Abstractions;
using MediatR;

namespace AiAgentsTeam.Application.Connectors.Queries;

public sealed record GetConnectorAuthorizeUrlQuery(Guid WorkspaceId, string ConnectorKey) : IRequest<string>;

public sealed class GetConnectorAuthorizeUrlQueryHandler(
    IConnectorRegistry registry, IConnectorOAuthStateSigner stateSigner, IConnectorConfig config)
    : IRequestHandler<GetConnectorAuthorizeUrlQuery, string>
{
    public Task<string> Handle(GetConnectorAuthorizeUrlQuery request, CancellationToken cancellationToken)
    {
        var connector = registry.Require(request.ConnectorKey);
        if (connector.OAuth is null)
            throw new InvalidOperationException($"Connector '{request.ConnectorKey}' does not use OAuth2.");

        var clientId = config.Require(connector.OAuth.ClientIdConfigKey);
        var redirectBaseUrl = config.Require("Connectors:RedirectBaseUrl");

        var redirectUri = $"{redirectBaseUrl.TrimEnd('/')}/api/connectors/{request.ConnectorKey}/oauth/callback";
        var state = stateSigner.Sign(request.WorkspaceId, request.ConnectorKey);

        var query = new List<string>
        {
            $"client_id={Uri.EscapeDataString(clientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
            $"response_type=code",
            $"scope={Uri.EscapeDataString(string.Join(' ', connector.OAuth.Scopes))}",
            $"state={Uri.EscapeDataString(state)}",
        };

        var separator = connector.OAuth.AuthorizeUrl.Contains('?') ? "&" : "?";
        return Task.FromResult($"{connector.OAuth.AuthorizeUrl}{separator}{string.Join('&', query)}");
    }
}
