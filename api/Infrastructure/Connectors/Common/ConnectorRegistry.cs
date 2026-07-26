using AiAgentsTeam.Application.Connectors.Abstractions;

namespace AiAgentsTeam.Infrastructure.Connectors.Common;

public sealed class ConnectorRegistry(IEnumerable<IConnectorDefinition> connectors) : IConnectorRegistry
{
    public IReadOnlyList<IConnectorDefinition> All { get; } = connectors.ToList();

    public IConnectorDefinition? Find(string key) => All.FirstOrDefault(c => c.Key == key);

    public IConnectorDefinition Require(string key) =>
        Find(key) ?? throw new KeyNotFoundException($"No connector registered with key '{key}'.");
}
