using AiAgentsTeam.Application.Artifacts.Commands;
using AiAgentsTeam.Application.Artifacts.Queries;
using AiAgentsTeam.Application.Common.Interfaces;
using AiAgentsTeam.Application.Common.Messaging;
using AiAgentsTeam.Application.Intent.Commands;
using AiAgentsTeam.Application.Memory.Commands;
using AiAgentsTeam.Application.Memory.Queries;
using AiAgentsTeam.Application.Reasoning.Commands;
using AiAgentsTeam.Application.Reasoning.Queries;
using AiAgentsTeam.Application.Registry.Commands;
using AiAgentsTeam.Application.Registry.Queries;
using AiAgentsTeam.Application.Supervisor.Commands;
using AiAgentsTeam.Application.Workflows.Commands;
using AiAgentsTeam.Application.Workflows.Queries;
using AiAgentsTeam.Domain.Artifacts;
using AiAgentsTeam.Domain.Memory;
using AiAgentsTeam.Domain.Reasoning;
using AiAgentsTeam.Domain.Supervisor;
using AiAgentsTeam.Domain.Workflow;
using AiAgentsTeam.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiAgentsTeam.IntegrationTests;

/// <summary>
/// End-to-end coverage of the core execution pipeline (Phase 1.5 §11), against
/// real Postgres + Redis (PipelineTestFixture) — no mocked persistence, no
/// mocked Event Bus. Each test uses unique names/goals so it can run alongside
/// the others against the same shared database without interference.
/// </summary>
[Collection("Pipeline")]
public sealed class WorkflowPipelineTests(PipelineTestFixture fixture)
{
    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task AgentRegistration_registers_and_heartbeats()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var agentName = Unique("agent");

        await sender.Send(new RegisterAgentCommand(
            agentName, "1.0.0", "test agent", ["skill"], ["TestTask"], 50,
            [], [], [], [], [], $"http://test/{agentName}", null, Guid.NewGuid()));

        var heartbeatOk = await sender.Send(new HeartbeatCommand(agentName));
        Assert.True(heartbeatOk);

        var agents = await sender.Send(new GetAgentsQuery());
        Assert.Contains(agents, a => a.Name == agentName && a.SupportedTasks.Contains("TestTask"));
    }

    [Fact]
    public async Task AgentRegistration_reregistering_same_name_updates_not_duplicates()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var agentName = Unique("agent");

        await sender.Send(new RegisterAgentCommand(
            agentName, "1.0.0", "v1", ["a"], ["TaskA"], 10, [], [], [], [], [], "http://test/1", null, Guid.NewGuid()));
        await sender.Send(new RegisterAgentCommand(
            agentName, "2.0.0", "v2", ["a", "b"], ["TaskA", "TaskB"], 20, [], [], [], [], [], "http://test/2", null, Guid.NewGuid()));

        var agents = await sender.Send(new GetAgentsQuery());
        var matches = agents.Where(a => a.Name == agentName).ToList();
        Assert.Single(matches);
        Assert.Equal("2.0.0", matches[0].Version);
        Assert.Equal(20, matches[0].Priority);
    }

    [Fact]
    public async Task WorkflowCreation_creates_run_with_correlation_id()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));

        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "test goal"));

        var run = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.NotNull(run);
        Assert.Equal("test goal", run!.Goal);
        Assert.NotEqual(Guid.Empty, run.CorrelationId);
        Assert.Equal("Planning", run.Status);
    }

    [Fact]
    public async Task IntentIntake_records_analysis_and_marks_structured()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));

        var sessionId = await sender.Send(new StartIntentSessionCommand(ws, "Build a CRM", Guid.NewGuid()));

        await sender.Send(new RecordIntentAnalysisCommand(
            sessionId, "{\"goals\":[]}", "SaaS", 0.5, "[]", "{}", false));

        var artifactId = await sender.Send(new CreateArtifactCommand(
            ws, "StructuredRequirements", ArtifactType.Markdown, "intent-engine", "content", null, null, null));

        await sender.Send(new MarkIntentStructuredCommand(sessionId, artifactId));

        var session = await db.IntentSessions.FirstAsync(s => s.Id == sessionId);
        Assert.Equal(AiAgentsTeam.Domain.Intent.IntentSessionStatus.Structured, session.Status);
        Assert.Equal("SaaS", session.ProjectClassification);
        Assert.Equal(artifactId, session.StructuredRequirementsArtifactId);
    }

    [Fact]
    public async Task DynamicDag_and_ParallelExecution_dispatches_independent_branches_together()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));

        var taskType = Unique("Task");
        var agentName = Unique("agent");
        await sender.Send(new RegisterAgentCommand(
            agentName, "1.0.0", "test", [], [taskType], 10, [], [], [], [], [], "http://test", null, ws));

        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "parallel test"));
        var root = await sender.Send(new AddTaskNodeCommand(runId, "Root", taskType, "{}"));
        var branchA = await sender.Send(new AddTaskNodeCommand(runId, "BranchA", taskType, "{}"));
        var branchB = await sender.Send(new AddTaskNodeCommand(runId, "BranchB", taskType, "{}"));
        await sender.Send(new AddTaskDependencyCommand(runId, root, branchA));
        await sender.Send(new AddTaskDependencyCommand(runId, root, branchB));

        await sender.Send(new StartWorkflowRunCommand(runId));

        var afterStart = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.Equal("Dispatched", afterStart!.Nodes.First(n => n.Id == root).Status);
        Assert.Equal("Pending", afterStart.Nodes.First(n => n.Id == branchA).Status);
        Assert.Equal("Pending", afterStart.Nodes.First(n => n.Id == branchB).Status);

        await sender.Send(new CompleteTaskCommand(root, "{}", 0.9, "low", "done"));

        // §5.2 step 3: both independent branches land in the same ready set and
        // are dispatched together — this is the concrete proof of parallel execution.
        var afterRootDone = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.Equal("Dispatched", afterRootDone!.Nodes.First(n => n.Id == branchA).Status);
        Assert.Equal("Dispatched", afterRootDone.Nodes.First(n => n.Id == branchB).Status);

        await sender.Send(new CompleteTaskCommand(branchA, "{}", 0.9, "low", "done"));
        await sender.Send(new CompleteTaskCommand(branchB, "{}", 0.9, "low", "done"));

        var final = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.Equal("Completed", final!.Status);
    }

    [Fact]
    public async Task DynamicDag_AddTaskNode_is_idempotent_by_name()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));
        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "idempotency test"));

        var firstId = await sender.Send(new AddTaskNodeCommand(runId, "SameName", "TaskType", "{}"));
        var secondId = await sender.Send(new AddTaskNodeCommand(runId, "SameName", "TaskType", "{}"));

        Assert.Equal(firstId, secondId);

        var run = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.Single(run!.Nodes, n => n.Name == "SameName");
    }

    [Fact]
    public async Task SupervisorDecisions_are_recorded_with_correlation_derived_from_run()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));
        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "supervisor test"));

        var decisionId = await sender.Send(new RecordSupervisorDecisionCommand(
            runId, SupervisorDecisionType.StrategySelection, "{}", "chose the fixed pipeline", 0.9, null));

        var decision = await db.SupervisorDecisions.FirstAsync(d => d.Id == decisionId);
        var run = await db.WorkflowRuns.FirstAsync(r => r.Id == runId);
        Assert.Equal(run.CorrelationId, decision.CorrelationId);
    }

    [Fact]
    public async Task Artifacts_are_versioned_and_never_overwritten()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));

        var v1 = await sender.Send(new CreateArtifactCommand(
            ws, "Doc", ArtifactType.Markdown, "agent", "version one", null, null, null));
        var v2 = await sender.Send(new CreateArtifactCommand(
            ws, "Doc", ArtifactType.Markdown, "agent", "version two", null, null, v1));

        var firstVersion = await sender.Send(new GetArtifactQuery(v1));
        var secondVersion = await sender.Send(new GetArtifactQuery(v2));

        Assert.Equal("Superseded", firstVersion!.Status);
        Assert.Equal("version one", firstVersion.Content);
        Assert.Equal(2, secondVersion!.Version);
        Assert.Equal(v1, secondVersion.PreviousVersionId);

        var history = await sender.Send(new GetArtifactVersionsQuery(v2));
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task Artifacts_by_name_resolves_latest_within_a_workflow_run()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));
        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "by-name test"));

        var id = await sender.Send(new CreateArtifactCommand(
            ws, "BackendCode", ArtifactType.Code, "backend-engineer", "code", runId, null, null));

        var resolved = await sender.Send(new GetLatestArtifactByNameQuery(runId, "BackendCode"));
        Assert.NotNull(resolved);
        Assert.Equal(id, resolved!.Id);
    }

    [Fact]
    public async Task Artifacts_creation_is_idempotent_by_key()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));
        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "idempotent artifact test"));
        var key = Unique("key");

        var first = await sender.Send(new CreateArtifactCommand(
            ws, "Doc", ArtifactType.Markdown, "agent", "content", runId, null, null, null, key));
        var second = await sender.Send(new CreateArtifactCommand(
            ws, "Doc", ArtifactType.Markdown, "agent", "different content", runId, null, null, null, key));

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Memory_write_and_recall_round_trips_by_layer_and_scope()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));
        var taskId = Guid.NewGuid();

        await sender.Send(new WriteMemoryItemCommand(
            ws, MemoryLayer.Working, taskId, MemoryKind.Decision, "remembered content", null, null));

        var results = await sender.Send(new QueryMemoryQuery(ws, MemoryLayer.Working, taskId));
        Assert.Contains(results, m => m.Content == "remembered content");
    }

    [Fact]
    public async Task ReasoningTraces_capture_full_stage_telemetry()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));
        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "trace test"));
        var nodeId = await sender.Send(new AddTaskNodeCommand(runId, "TracedNode", "Task", "{}"));

        await sender.Send(new RecordReasoningTraceCommand(
            nodeId, "test-agent", ReasoningStage.Execute, DateTimeOffset.UtcNow, 42,
            null, "output", 100, 0.9, "mock-model", 0, 1, 1, 2, 0.001, null));

        var traces = await sender.Send(new GetReasoningTracesQuery(nodeId));
        var trace = Assert.Single(traces);
        Assert.Equal("Execute", trace.Stage);
        Assert.Equal(100, trace.Tokens);
        Assert.Equal("mock-model", trace.ModelUsed);
        Assert.Equal(2, trace.ToolCalls);
        Assert.NotEqual(Guid.Empty, trace.WorkflowRunId);
    }

    [Fact]
    public async Task Retries_reset_node_to_pending_until_max_attempts_then_fails_the_run()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));

        var taskType = Unique("Task");
        var agentName = Unique("agent");
        await sender.Send(new RegisterAgentCommand(
            agentName, "1.0.0", "test", [], [taskType], 10, [], [], [], [], [], "http://test", null, ws));

        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "retry test"));
        var nodeId = await sender.Send(new AddTaskNodeCommand(runId, "FlakyNode", taskType, "{}"));
        await sender.Send(new StartWorkflowRunCommand(runId));

        // Attempt 1 fails (retryable) -> reset to Pending -> re-dispatched (attempt 2).
        await sender.Send(new FailTaskCommand(nodeId, null));
        var afterFirstFailure = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.Equal("Dispatched", afterFirstFailure!.Nodes.First(n => n.Id == nodeId).Status);
        Assert.Equal(2, afterFirstFailure.Nodes.First(n => n.Id == nodeId).AttemptCount);

        // Attempt 2 fails -> MaxTaskAttempts (2) reached -> terminal failure, run fails.
        await sender.Send(new FailTaskCommand(nodeId, null));
        var afterSecondFailure = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.Equal("Failed", afterSecondFailure!.Nodes.First(n => n.Id == nodeId).Status);
        Assert.Equal("Failed", afterSecondFailure.Status);
    }

    [Fact]
    public async Task Retries_are_skipped_for_non_retryable_structured_failures()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));

        var taskType = Unique("Task");
        var agentName = Unique("agent");
        await sender.Send(new RegisterAgentCommand(
            agentName, "1.0.0", "test", [], [taskType], 10, [], [], [], [], [], "http://test", null, ws));

        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "non-retryable test"));
        var nodeId = await sender.Send(new AddTaskNodeCommand(runId, "PermissionDenied", taskType, "{}"));
        await sender.Send(new StartWorkflowRunCommand(runId));

        var reasonJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            category = "Permission",
            severity = "High",
            recoverable = false,
            retryable = false,
            message = "denied",
            stack = (string?)null,
            suggestedAction = "check creds",
        });

        await sender.Send(new FailTaskCommand(nodeId, reasonJson));

        var run = await sender.Send(new GetWorkflowRunQuery(runId));
        Assert.Equal("Failed", run!.Nodes.First(n => n.Id == nodeId).Status);
        Assert.Equal(1, run.Nodes.First(n => n.Id == nodeId).AttemptCount); // no retry attempted
        Assert.Equal("Failed", run.Status);

        var failure = await db.ExecutionFailures.FirstAsync(f => f.TaskNodeId == nodeId);
        Assert.Equal(AiAgentsTeam.Domain.Failures.FailureCategory.Permission, failure.Category);
        Assert.False(failure.Retryable);
        Assert.Equal("check creds", failure.SuggestedAction);
    }

    [Fact]
    public async Task ExecutionSnapshots_are_written_on_every_scheduling_pass()
    {
        using var scope = fixture.NewScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var ws = await sender.Send(new AiAgentsTeam.Application.Workspaces.Commands.CreateWorkspaceCommand(Unique("ws")));

        var taskType = Unique("Task");
        var agentName = Unique("agent");
        await sender.Send(new RegisterAgentCommand(
            agentName, "1.0.0", "test", [], [taskType], 10, [], [], [], [], [], "http://test", null, ws));

        var runId = await sender.Send(new CreateWorkflowRunCommand(ws, "snapshot test"));
        var nodeId = await sender.Send(new AddTaskNodeCommand(runId, "OnlyNode", taskType, "{}"));
        await sender.Send(new StartWorkflowRunCommand(runId));
        await sender.Send(new CompleteTaskCommand(nodeId, "{}", 0.9, "low", "done"));

        var checkpoints = await db.Checkpoints.Where(c => c.WorkflowRunId == runId).ToListAsync();
        Assert.True(checkpoints.Count >= 2);
        Assert.Contains(checkpoints, c => c.Label == "workflow-completed");
    }
}
