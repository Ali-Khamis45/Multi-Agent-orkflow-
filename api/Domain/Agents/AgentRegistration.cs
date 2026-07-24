using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Agents;

/// <summary>
/// A row in the Dynamic Agent Registry (ARCHITECTURE.md §4). Agents self-register
/// via POST /api/registry/agents with this manifest shape; the Supervisor/Scheduler
/// never hardcodes agent identity, only resolves candidates from this table.
/// </summary>
public class AgentRegistration : Entity
{
    public string Name { get; private set; } = default!;
    public string Version { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    public List<string> Skills { get; private set; } = new();
    public List<string> SupportedTasks { get; private set; } = new();
    public int Priority { get; private set; }
    public List<string> RequiredContext { get; private set; } = new();
    public List<string> ProducedArtifacts { get; private set; } = new();
    public List<string> Dependencies { get; private set; } = new();
    public List<string> Tools { get; private set; } = new();
    public List<string> Permissions { get; private set; } = new();

    public string Endpoint { get; private set; } = default!;
    public string? HealthCheck { get; private set; }

    public AgentStatus Status { get; private set; } = AgentStatus.Available;
    public DateTimeOffset LastHeartbeatAt { get; private set; } = DateTimeOffset.UtcNow;
    public int InFlightTaskCount { get; private set; }

    private AgentRegistration() { }

    public AgentRegistration(
        string name,
        string version,
        string description,
        List<string> skills,
        List<string> supportedTasks,
        int priority,
        List<string> requiredContext,
        List<string> producedArtifacts,
        List<string> dependencies,
        List<string> tools,
        List<string> permissions,
        string endpoint,
        string? healthCheck)
    {
        Name = name;
        Version = version;
        Description = description;
        Skills = skills;
        SupportedTasks = supportedTasks;
        Priority = priority;
        RequiredContext = requiredContext;
        ProducedArtifacts = producedArtifacts;
        Dependencies = dependencies;
        Tools = tools;
        Permissions = permissions;
        Endpoint = endpoint;
        HealthCheck = healthCheck;
    }

    /// <summary>Re-registration (new deploy of the same agent name) refreshes the whole manifest.</summary>
    public void UpdateManifest(
        string version,
        string description,
        List<string> skills,
        List<string> supportedTasks,
        int priority,
        List<string> requiredContext,
        List<string> producedArtifacts,
        List<string> dependencies,
        List<string> tools,
        List<string> permissions,
        string endpoint,
        string? healthCheck)
    {
        Version = version;
        Description = description;
        Skills = skills;
        SupportedTasks = supportedTasks;
        Priority = priority;
        RequiredContext = requiredContext;
        ProducedArtifacts = producedArtifacts;
        Dependencies = dependencies;
        Tools = tools;
        Permissions = permissions;
        Endpoint = endpoint;
        HealthCheck = healthCheck;
        Status = AgentStatus.Available;
        LastHeartbeatAt = DateTimeOffset.UtcNow;
    }

    public void Heartbeat()
    {
        LastHeartbeatAt = DateTimeOffset.UtcNow;
        if (Status == AgentStatus.Unavailable)
            Status = AgentStatus.Available;
    }

    public void MarkUnavailable() => Status = AgentStatus.Unavailable;

    public void IncrementInFlight() => InFlightTaskCount++;

    public void DecrementInFlight() => InFlightTaskCount = Math.Max(0, InFlightTaskCount - 1);

    public bool Supports(string taskType) => SupportedTasks.Contains(taskType);

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
