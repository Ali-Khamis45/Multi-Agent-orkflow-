using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiAgentsTeam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_Agent_CompanyType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_agent_registrations_Name",
                table: "agent_registrations");

            // Every agent registered before this migration was, definitionally, a
            // Software Company agent — Founder agents didn't exist yet — so backfill
            // to that rather than EF's auto-generated "" default, which would
            // silently make pre-existing agents invisible to any CompanyType filter.
            migrationBuilder.AddColumn<string>(
                name: "CompanyType",
                table: "agent_registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "SoftwareCompany");

            migrationBuilder.CreateIndex(
                name: "IX_agent_registrations_Name_CompanyType",
                table: "agent_registrations",
                columns: new[] { "Name", "CompanyType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_agent_registrations_Name_CompanyType",
                table: "agent_registrations");

            migrationBuilder.DropColumn(
                name: "CompanyType",
                table: "agent_registrations");

            migrationBuilder.CreateIndex(
                name: "IX_agent_registrations_Name",
                table: "agent_registrations",
                column: "Name",
                unique: true);
        }
    }
}
