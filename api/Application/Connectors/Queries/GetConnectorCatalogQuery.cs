using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Users;
using MediatR;

namespace AiAgentsTeam.Application.Connectors.Queries;

public sealed record ConnectorCatalogEntryDto(
    string Key,
    string DisplayName,
    string Description,
    string AuthType,
    bool OAuthAvailable,
    IReadOnlyList<string> RequiredCredentialFields,
    IReadOnlyList<ConnectorActionDefinition> Actions,
    IReadOnlyList<string> Events);

/// <summary>The Connector Marketplace's "browse" list — scoped to the caller's own
/// CompanyType, same principle as GetAgentsQuery (Phase 2): a Founder never sees GitHub,
/// a Software user never sees Shopify.</summary>
public sealed record GetConnectorCatalogQuery(CompanyType CompanyType) : IRequest<IReadOnlyList<ConnectorCatalogEntryDto>>;

public sealed class GetConnectorCatalogQueryHandler(IConnectorRegistry registry)
    : IRequestHandler<GetConnectorCatalogQuery, IReadOnlyList<ConnectorCatalogEntryDto>>
{
    public Task<IReadOnlyList<ConnectorCatalogEntryDto>> Handle(GetConnectorCatalogQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ConnectorCatalogEntryDto> result = registry.All
            .Where(c => c.CompanyType == request.CompanyType)
            .Select(c => new ConnectorCatalogEntryDto(
                c.Key, c.DisplayName, c.Description, c.AuthType.ToString(), c.OAuth is not null,
                c.RequiredCredentialFields, c.Actions, c.Events))
            .OrderBy(c => c.DisplayName)
            .ToList();

        return Task.FromResult(result);
    }
}
