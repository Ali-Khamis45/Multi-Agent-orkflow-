using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Connectors;

public enum ConnectorInstallationStatus { Connected, Disconnected, Error }

/// <summary>
/// Phase 4 ("Connector Framework") — a Workspace's connection to one external system
/// (Shopify, GitHub, Stripe, ...). The <see cref="ConnectorKey"/> is the only thing
/// that ties this row to a specific integration; everything connector-specific (which
/// API to call, what the OAuth scopes are, what an action does) lives in that
/// connector's own <c>IConnectorDefinition</c> implementation (Application/Infrastructure
/// layers) — this entity only knows "some connector is installed, here is its encrypted
/// credential blob and its status," which is exactly what keeps connector-specific logic
/// out of the core persistence model.
/// </summary>
public class ConnectorInstallation : Entity
{
    public Guid WorkspaceId { get; private set; }
    public string ConnectorKey { get; private set; } = default!;
    public ConnectorInstallationStatus Status { get; private set; } = ConnectorInstallationStatus.Disconnected;

    /// <summary>Encrypted via ICredentialProtector (ASP.NET Core Data Protection) before
    /// it ever reaches this entity — never plaintext at rest. Null until the first
    /// successful install.</summary>
    public string? EncryptedCredentialsJson { get; private set; }

    public DateTimeOffset? LastHealthCheckAt { get; private set; }
    public bool? LastHealthOk { get; private set; }
    public string? LastHealthMessage { get; private set; }

    public DateTimeOffset? LastSyncedAt { get; private set; }
    public bool? LastSyncOk { get; private set; }
    public string? LastSyncMessage { get; private set; }

    private ConnectorInstallation() { }

    public ConnectorInstallation(Guid workspaceId, string connectorKey)
    {
        WorkspaceId = workspaceId;
        ConnectorKey = connectorKey;
    }

    public void Connect(string encryptedCredentialsJson)
    {
        EncryptedCredentialsJson = encryptedCredentialsJson;
        Status = ConnectorInstallationStatus.Connected;
    }

    public void Disconnect()
    {
        EncryptedCredentialsJson = null;
        Status = ConnectorInstallationStatus.Disconnected;
        LastHealthCheckAt = null;
        LastHealthOk = null;
        LastHealthMessage = null;
    }

    public void RecordHealth(bool ok, string message)
    {
        LastHealthCheckAt = DateTimeOffset.UtcNow;
        LastHealthOk = ok;
        LastHealthMessage = message;
        if (!ok && Status == ConnectorInstallationStatus.Connected)
            Status = ConnectorInstallationStatus.Error;
        else if (ok && Status == ConnectorInstallationStatus.Error)
            Status = ConnectorInstallationStatus.Connected;
    }

    public void RecordSync(bool ok, string message)
    {
        LastSyncedAt = DateTimeOffset.UtcNow;
        LastSyncOk = ok;
        LastSyncMessage = message;
    }
}
