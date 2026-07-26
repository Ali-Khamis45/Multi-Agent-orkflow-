using System.Reflection;
using AiAgentsTeam.Application.Common.Interfaces;
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

namespace AiAgentsTeam.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AgentRegistration> AgentRegistrations => Set<AgentRegistration>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<TaskNode> TaskNodes => Set<TaskNode>();
    public DbSet<TaskEdge> TaskEdges => Set<TaskEdge>();
    public DbSet<Artifact> Artifacts => Set<Artifact>();
    public DbSet<Checkpoint> Checkpoints => Set<Checkpoint>();
    public DbSet<MemoryItem> MemoryItems => Set<MemoryItem>();
    public DbSet<SupervisorDecision> SupervisorDecisions => Set<SupervisorDecision>();
    public DbSet<ReasoningTrace> ReasoningTraces => Set<ReasoningTrace>();
    public DbSet<IntentSession> IntentSessions => Set<IntentSession>();
    public DbSet<ClarificationAnswer> ClarificationAnswers => Set<ClarificationAnswer>();
    public DbSet<ExecutionFailure> ExecutionFailures => Set<ExecutionFailure>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<ConnectorInstallation> ConnectorInstallations => Set<ConnectorInstallation>();
    public DbSet<ConnectorActionLog> ConnectorActionLogs => Set<ConnectorActionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public void Detach(object entity) => Entry(entity).State = EntityState.Detached;
}
