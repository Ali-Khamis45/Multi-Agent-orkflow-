using AiAgentsTeam.Application.Connectors.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AiAgentsTeam.Infrastructure.Connectors.Common;

public sealed class ConnectorConfig(IConfiguration configuration) : IConnectorConfig
{
    public string? Get(string key) => configuration[key];

    public string Require(string key) => Get(key) ?? throw new InvalidOperationException($"Missing configuration '{key}'.");
}
