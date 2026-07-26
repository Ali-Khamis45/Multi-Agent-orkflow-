using AiAgentsTeam.Domain.Agents;
using AiAgentsTeam.Domain.Artifacts;
using AiAgentsTeam.Domain.Checkpoints;
using AiAgentsTeam.Domain.Connectors;
using AiAgentsTeam.Domain.Failures;
using AiAgentsTeam.Domain.Founders;
using AiAgentsTeam.Domain.Intent;
using AiAgentsTeam.Domain.Memory;
using AiAgentsTeam.Domain.Reasoning;
using AiAgentsTeam.Domain.Supervisor;
using AiAgentsTeam.Domain.Users;
using AiAgentsTeam.Domain.Workflow;
using AiAgentsTeam.Domain.Workspaces;
using Microsoft.EntityFrameworkCore;

namespace AiAgentsTeam.Application.Common.Interfaces;

/// <summary>
/// The Application layer's only view of persistence — Infrastructure implements
/// this against Postgres via EF Core. Handlers depend on this interface, never on
/// EF Core or Npgsql directly (Clean Architecture dependency rule).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Workspace> Workspaces { get; }
    DbSet<User> Users { get; }
    DbSet<AgentRegistration> AgentRegistrations { get; }
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowRun> WorkflowRuns { get; }
    DbSet<TaskNode> TaskNodes { get; }
    DbSet<TaskEdge> TaskEdges { get; }
    DbSet<Artifact> Artifacts { get; }
    DbSet<Checkpoint> Checkpoints { get; }
    DbSet<MemoryItem> MemoryItems { get; }
    DbSet<SupervisorDecision> SupervisorDecisions { get; }
    DbSet<ReasoningTrace> ReasoningTraces { get; }
    DbSet<IntentSession> IntentSessions { get; }
    DbSet<ClarificationAnswer> ClarificationAnswers { get; }
    DbSet<ExecutionFailure> ExecutionFailures { get; }
    DbSet<CompanyProfile> CompanyProfiles { get; }
    DbSet<ConnectorInstallation> ConnectorInstallations { get; }
    DbSet<ConnectorActionLog> ConnectorActionLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Evicts a stale tracked entity from the change tracker after a
    /// DbUpdateConcurrencyException so a subsequent query for the same key actually
    /// re-hits the database instead of returning the identity map's stale instance —
    /// see PatchCompanyProfileSectionCommand's retry loop.</summary>
    void Detach(object entity);
}
