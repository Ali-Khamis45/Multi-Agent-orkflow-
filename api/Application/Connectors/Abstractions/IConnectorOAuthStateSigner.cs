namespace AiAgentsTeam.Application.Connectors.Abstractions;

/// <summary>Signs/verifies the OAuth `state` param so CompleteConnectorOAuthCommand can
/// trust which Workspace + connector a callback belongs to without a server-side session
/// — the state param is the only thing round-tripped through the third-party provider,
/// so it has to be self-verifying (HMAC) rather than a lookup key into mutable state.</summary>
public interface IConnectorOAuthStateSigner
{
    string Sign(Guid workspaceId, string connectorKey);

    /// <summary>Null if the state is missing, malformed, tampered with, or expired.</summary>
    (Guid WorkspaceId, string ConnectorKey)? Verify(string state);
}
