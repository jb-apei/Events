using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectionService.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToProspectSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ProspectSummary",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ProspectSummary");
        }
    }
}
