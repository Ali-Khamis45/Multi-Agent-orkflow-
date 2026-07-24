using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgentsTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_registrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Skills = table.Column<List<string>>(type: "text[]", nullable: false),
                    SupportedTasks = table.Column<List<string>>(type: "text[]", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RequiredContext = table.Column<List<string>>(type: "text[]", nullable: false),
                    ProducedArtifacts = table.Column<List<string>>(type: "text[]", nullable: false),
                    Dependencies = table.Column<List<string>>(type: "text[]", nullable: false),
                    Tools = table.Column<List<string>>(type: "text[]", nullable: false),
                    Permissions = table.Column<List<string>>(type: "text[]", nullable: false),
                    Endpoint = table.Column<string>(type: "text", nullable: false),
                    HealthCheck = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastHeartbeatAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InFlightTaskCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerAgent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    StorageRef = table.Column<string>(type: "text", nullable: true),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_artifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "checkpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checkpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "intent_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawInput = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExtractedGoalsJson = table.Column<string>(type: "text", nullable: true),
                    ProjectClassification = table.Column<string>(type: "text", nullable: true),
                    ComplexityScore = table.Column<double>(type: "double precision", nullable: true),
                    RiskFlagsJson = table.Column<string>(type: "text", nullable: true),
                    AmbiguitiesJson = table.Column<string>(type: "text", nullable: true),
                    StructuredRequirementsArtifactId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intent_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "memory_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Layer = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScopeRef = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SourceArtifactId = table.Column<Guid>(type: "uuid", nullable: true),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    TtlAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    SupersededById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memory_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reasoning_traces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Agent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InputJson = table.Column<string>(type: "text", nullable: true),
                    OutputJson = table.Column<string>(type: "text", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reasoning_traces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "supervisor_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    TargetNodeIdsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supervisor_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Goal = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "clarification_answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntentSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clarification_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clarification_answers_intent_sessions_IntentSessionId",
                        column: x => x.IntentSessionId,
                        principalTable: "intent_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_edges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PredecessorNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuccessorNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_edges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_edges_workflow_runs_WorkflowRunId",
                        column: x => x.WorkflowRunId,
                        principalTable: "workflow_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "task_nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParentNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaskType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedAgentName = table.Column<string>(type: "text", nullable: true),
                    InputsJson = table.Column<string>(type: "text", nullable: true),
                    OutputsJson = table.Column<string>(type: "text", nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    RiskLevel = table.Column<string>(type: "text", nullable: true),
                    ReasoningSummary = table.Column<string>(type: "text", nullable: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_nodes_workflow_runs_WorkflowRunId",
                        column: x => x.WorkflowRunId,
                        principalTable: "workflow_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agent_registrations_Name",
                table: "agent_registrations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_artifacts_PreviousVersionId",
                table: "artifacts",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_artifacts_WorkspaceId_Name",
                table: "artifacts",
                columns: new[] { "WorkspaceId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_checkpoints_WorkflowRunId",
                table: "checkpoints",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_clarification_answers_IntentSessionId",
                table: "clarification_answers",
                column: "IntentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_intent_sessions_WorkspaceId",
                table: "intent_sessions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_memory_items_WorkspaceId_Layer_ScopeRef",
                table: "memory_items",
                columns: new[] { "WorkspaceId", "Layer", "ScopeRef" });

            migrationBuilder.CreateIndex(
                name: "IX_reasoning_traces_TaskNodeId_Stage",
                table: "reasoning_traces",
                columns: new[] { "TaskNodeId", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_decisions_WorkflowRunId",
                table: "supervisor_decisions",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_task_edges_PredecessorNodeId",
                table: "task_edges",
                column: "PredecessorNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_task_edges_SuccessorNodeId",
                table: "task_edges",
                column: "SuccessorNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_task_edges_WorkflowRunId",
                table: "task_edges",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_task_nodes_WorkflowRunId_Status",
                table: "task_nodes",
                columns: new[] { "WorkflowRunId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runs_WorkspaceId",
                table: "workflow_runs",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_registrations");

            migrationBuilder.DropTable(
                name: "artifacts");

            migrationBuilder.DropTable(
                name: "checkpoints");

            migrationBuilder.DropTable(
                name: "clarification_answers");

            migrationBuilder.DropTable(
                name: "memory_items");

            migrationBuilder.DropTable(
                name: "reasoning_traces");

            migrationBuilder.DropTable(
                name: "supervisor_decisions");

            migrationBuilder.DropTable(
                name: "task_edges");

            migrationBuilder.DropTable(
                name: "task_nodes");

            migrationBuilder.DropTable(
                name: "workflow_definitions");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropTable(
                name: "intent_sessions");

            migrationBuilder.DropTable(
                name: "workflow_runs");
        }
    }
}
