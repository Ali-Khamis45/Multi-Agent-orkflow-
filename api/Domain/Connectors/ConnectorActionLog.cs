using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Connectors;

/// <summary>
/// Audit trail of every real-world action the platform has taken through a connector
/// (Phase 4: "The AI should not only generate recommendations. It should perform real
/// actions.") — a founder or engineer can always answer "what did the AI actually do
/// to my Shopify store / GitHub repo," which matters a great deal more than for a
/// generated artifact once actions have external, hard-to-undo side effects.
/// </summary>
public class ConnectorActionLog : Entity
{
    public Guid WorkspaceId { get; private set; }
    public string ConnectorKey { get; private set; } = default!;
    public string ActionKey { get; private set; } = default!;
    public string InputJson { get; private set; } = default!;
    public string? OutputJson { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? CorrelationId { get; private set; }

    private ConnectorActionLog() { }

    public ConnectorActionLog(
        Guid workspaceId, string connectorKey, string actionKey, string inputJson,
        bool success, string? outputJson, string? errorMessage, Guid? correlationId)
    {
        WorkspaceId = workspaceId;
        ConnectorKey = connectorKey;
        ActionKey = actionKey;
        InputJson = inputJson;
        Success = success;
        OutputJson = outputJson;
        ErrorMessage = errorMessage;
        CorrelationId = correlationId;
    }
}
