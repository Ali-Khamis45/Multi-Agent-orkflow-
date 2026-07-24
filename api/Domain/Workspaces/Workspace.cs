using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Workspaces;

/// <summary>
/// Top-level tenant boundary (ARCHITECTURE.md §3, §19). Every workflow, agent
/// registration, artifact, and memory item is scoped by WorkspaceId.
/// </summary>
public class Workspace : Entity
{
    public string Name { get; private set; } = default!;

    private Workspace() { }

    public Workspace(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name is required.", nameof(name));

        Name = name;
    }
}
