using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Connectors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Connectors.Commands;

/// <summary>Handles both install paths: a user submitting API-key credentials directly
/// (Shopify storeUrl+apiKey, etc.) and CompleteConnectorOAuthCommand handing off an
/// exchanged token — both end up as "here is a credential dictionary, store it
/// encrypted." Idempotent per (WorkspaceId, ConnectorKey): reconnecting overwrites the
/// previous credentials rather than erroring.</summary>
public sealed record InstallConnectorCommand(Guid WorkspaceId, string ConnectorKey, IReadOnlyDictionary<string, string> Credentials) : IRequest<Unit>;

public sealed class InstallConnectorCommandHandler(IApplicationDbContext db, IConnectorRegistry registry, ICredentialProtector protector)
    : IRequestHandler<InstallConnectorCommand, Unit>
{
    public async Task<Unit> Handle(InstallConnectorCommand request, CancellationToken cancellationToken)
    {
        var connector = registry.Require(request.ConnectorKey);

        if (connector.AuthType == ConnectorAuthType.ApiKey)
        {
            var missing = connector.RequiredCredentialFields.Where(f => !request.Credentials.ContainsKey(f)).ToList();
            if (missing.Count > 0)
                throw new ArgumentException($"Missing required credential field(s): {string.Join(", ", missing)}.");
        }

        var installation = await db.ConnectorInstallations
            .FirstOrDefaultAsync(c => c.WorkspaceId == request.WorkspaceId && c.ConnectorKey == request.ConnectorKey, cancellationToken);
        if (installation is null)
        {
            installation = new ConnectorInstallation(request.WorkspaceId, request.ConnectorKey);
            db.ConnectorInstallations.Add(installation);
        }

        var encrypted = protector.Protect(JsonSerializer.Serialize(request.Credentials));
        installation.Connect(encrypted);

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
