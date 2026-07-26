namespace AiAgentsTeam.Application.Connectors.Abstractions;

/// <summary>DI-populated catalog of every registered <see cref="IConnectorDefinition"/> —
/// analogous to the Python AI Runtime's AGENT_CLASSES list, just resolved through the
/// .NET container instead of a literal list, since connectors live in Infrastructure
/// (they make real HTTP calls) while the registry contract lives here in Application.</summary>
public interface IConnectorRegistry
{
    IReadOnlyList<IConnectorDefinition> All { get; }
    IConnectorDefinition? Find(string key);
    IConnectorDefinition Require(string key);
}
