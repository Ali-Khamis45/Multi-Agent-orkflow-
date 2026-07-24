using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgentsTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_5_Hardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "workflow_runs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "task_nodes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "supervisor_decisions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "reasoning_traces",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "reasoning_traces",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<double>(
                name: "CostEstimate",
                table: "reasoning_traces",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "reasoning_traces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemoryReads",
                table: "reasoning_traces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MemoryWrites",
                table: "reasoning_traces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModelUsed",
                table: "reasoning_traces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "reasoning_traces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                table: "reasoning_traces",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "Tokens",
                table: "reasoning_traces",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToolCalls",
                table: "reasoning_traces",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkflowRunId",
                table: "reasoning_traces",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "memory_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "intent_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "checkpoints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "artifacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "artifacts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "execution_failures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Agent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Recoverable = table.Column<bool>(type: "boolean", nullable: false),
                    Retryable = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Stack = table.Column<string>(type: "text", nullable: true),
                    SuggestedAction = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_execution_failures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runs_CorrelationId",
                table: "workflow_runs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_task_nodes_CorrelationId",
                table: "task_nodes",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_task_nodes_WorkflowRunId_Name",
                table: "task_nodes",
                columns: new[] { "WorkflowRunId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supervisor_decisions_CorrelationId",
                table: "supervisor_decisions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_reasoning_traces_Agent",
                table: "reasoning_traces",
                column: "Agent");

            migrationBuilder.CreateIndex(
                name: "IX_reasoning_traces_CorrelationId",
                table: "reasoning_traces",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_reasoning_traces_WorkflowRunId",
                table: "reasoning_traces",
                column: "WorkflowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_memory_items_CorrelationId",
                table: "memory_items",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_intent_sessions_CorrelationId",
                table: "intent_sessions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_checkpoints_CorrelationId",
                table: "checkpoints",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_artifacts_CorrelationId",
                table: "artifacts",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_artifacts_WorkflowRunId_IdempotencyKey",
                table: "artifacts",
                columns: new[] { "WorkflowRunId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_execution_failures_Agent_Category",
                table: "execution_failures",
                columns: new[] { "Agent", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_execution_failures_CorrelationId",
                table: "execution_failures",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_execution_failures_TaskNodeId",
                table: "execution_failures",
                column: "TaskNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_execution_failures_WorkflowRunId",
                table: "execution_failures",
                column: "WorkflowRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "execution_failures");

            migrationBuilder.DropIndex(
                name: "IX_workflow_runs_CorrelationId",
                table: "workflow_runs");

            migrationBuilder.DropIndex(
                name: "IX_task_nodes_CorrelationId",
                table: "task_nodes");

            migrationBuilder.DropIndex(
                name: "IX_task_nodes_WorkflowRunId_Name",
                table: "task_nodes");

            migrationBuilder.DropIndex(
                name: "IX_supervisor_decisions_CorrelationId",
                table: "supervisor_decisions");

            migrationBuilder.DropIndex(
                name: "IX_reasoning_traces_Agent",
                table: "reasoning_traces");

            migrationBuilder.DropIndex(
                name: "IX_reasoning_traces_CorrelationId",
                table: "reasoning_traces");

            migrationBuilder.DropIndex(
                name: "IX_reasoning_traces_WorkflowRunId",
                table: "reasoning_traces");

            migrationBuilder.DropIndex(
                name: "IX_memory_items_CorrelationId",
                table: "memory_items");

            migrationBuilder.DropIndex(
                name: "IX_intent_sessions_CorrelationId",
                table: "intent_sessions");

            migrationBuilder.DropIndex(
                name: "IX_checkpoints_CorrelationId",
                table: "checkpoints");

            migrationBuilder.DropIndex(
                name: "IX_artifacts_CorrelationId",
                table: "artifacts");

            migrationBuilder.DropIndex(
                name: "IX_artifacts_WorkflowRunId_IdempotencyKey",
                table: "artifacts");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "workflow_runs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "task_nodes");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "supervisor_decisions");

            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "CostEstimate",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "MemoryReads",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "MemoryWrites",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "ModelUsed",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "Tokens",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "ToolCalls",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "WorkflowRunId",
                table: "reasoning_traces");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "memory_items");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "intent_sessions");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "checkpoints");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "artifacts");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "artifacts");
        }
    }
}
