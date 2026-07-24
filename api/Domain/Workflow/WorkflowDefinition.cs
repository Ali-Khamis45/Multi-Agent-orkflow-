using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Workflow;

/// <summary>
/// A named, versioned workflow template (ARCHITECTURE.md §5.1; catalog/instantiation
/// mechanics belong to the Workflow Template Library, ARCHITECTURE_EXTENSION.md §E9).
/// Optional — a WorkflowRun can also be built ad hoc by the Supervisor without one.
/// </summary>
public class WorkflowDefinition : Entity
{
    public string Name { get; private set; } = default!;
    public string Version { get; private set; } = default!;
    public string? Category { get; private set; }

    private WorkflowDefinition() { }

    public WorkflowDefinition(string name, string version, string? category = null)
    {
        Name = name;
        Version = version;
        Category = category;
    }
}
