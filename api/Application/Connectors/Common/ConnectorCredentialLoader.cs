using System.Text.Json;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Connectors.Abstractions;
using AiAgentsTeam.Domain.Connectors;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Connectors.Common;

/// <summary>Shared "load the installation, decrypt its credentials, resolve its
/// connector definition" sequence used by every command that actually calls out to a
/// connector (health check, sync, execute action) — kept in one place so those three
/// handlers don't each reimplement the same lookup-and-decrypt steps.</summary>
public sealed class ConnectorCredentialLoader(IApplicationDbContext db, IConnectorRegistry registry, ICredentialProtector protector)
{
    public async Task<(ConnectorInstallation Installation, IConnectorDefinition Connector, ConnectorCredentials Credentials)> LoadAsync(
        Guid workspaceId, string connectorKey, CancellationToken ct)
    {
        var installation = await db.ConnectorInstallations
            .FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.ConnectorKey == connectorKey, ct)
            ?? throw new KeyNotFoundException($"Connector '{connectorKey}' is not installed for this workspace.");

        if (installation.EncryptedCredentialsJson is null)
            throw new InvalidOperationException($"Connector '{connectorKey}' has no stored credentials.");

        var connector = registry.Require(connectorKey);
        var decrypted = protector.Unprotect(installation.EncryptedCredentialsJson);
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted) ?? [];

        return (installation, connector, new ConnectorCredentials(values));
    }
}
