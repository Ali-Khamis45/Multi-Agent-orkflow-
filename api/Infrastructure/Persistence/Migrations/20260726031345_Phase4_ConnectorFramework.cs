using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgentsTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_ConnectorFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "connector_action_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InputJson = table.Column<string>(type: "text", nullable: false),
                    OutputJson = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connector_action_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "connector_installations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectorKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EncryptedCredentialsJson = table.Column<string>(type: "text", nullable: true),
                    LastHealthCheckAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastHealthOk = table.Column<bool>(type: "boolean", nullable: true),
                    LastHealthMessage = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastSyncOk = table.Column<bool>(type: "boolean", nullable: true),
                    LastSyncMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connector_installations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_connector_action_logs_WorkspaceId_ConnectorKey_CreatedAt",
                table: "connector_action_logs",
                columns: new[] { "WorkspaceId", "ConnectorKey", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_connector_installations_WorkspaceId_ConnectorKey",
                table: "connector_installations",
                columns: new[] { "WorkspaceId", "ConnectorKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "connector_action_logs");

            migrationBuilder.DropTable(
                name: "connector_installations");
        }
    }
}
