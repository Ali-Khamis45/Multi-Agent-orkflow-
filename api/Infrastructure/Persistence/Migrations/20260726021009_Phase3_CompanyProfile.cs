using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgentsTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_CompanyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOnboarded = table.Column<bool>(type: "boolean", nullable: false),
                    ProfileJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_profiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_profiles_WorkspaceId",
                table: "company_profiles",
                column: "WorkspaceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_profiles");
        }
    }
}
