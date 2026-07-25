using AiAgentsTeam.Domain.Common;

namespace AiAgentsTeam.Domain.Workspaces;

/// <summary>
/// Top-level tenant boundary (ARCHITECTURE.md §3, §19). Every workflow, agent
/// registration, artifact, and memory item is scoped by WorkspaceId. Unrelated to
/// <see cref="Domain.Users.CompanyType"/> — a Workspace is a named project/container a
/// user works inside; CompanyType decides which product (Mission Control vs. the
/// Founder workspace) a user is routed into in the first place. See
/// docs/architecture/OVERVIEW.md for the distinction.
/// </summary>
public class Workspace : Entity
{
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Owning user, nullable only because Workspaces created before Phase 2's auth
    /// system existed have no owner to backfill — every Workspace created from
    /// registration onward always has one.
    /// </summary>
    public Guid? UserId { get; private set; }

    private Workspace() { }

    public Workspace(string name, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name is required.", nameof(name));

        Name = name;
        UserId = userId;
    }
}
