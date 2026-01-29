using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectionService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inbox",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inbox", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "ProspectSummary",
                columns: table => new
                {
                    ProspectId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProspectSummary", x => x.ProspectId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_EventType",
                table: "Inbox",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_Inbox_ProcessedAt",
                table: "Inbox",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectSummary_CreatedAt",
                table: "ProspectSummary",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectSummary_Email",
                table: "ProspectSummary",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProspectSummary_Status",
                table: "ProspectSummary",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProspectSummary_UpdatedAt",
                table: "ProspectSummary",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inbox");

            migrationBuilder.DropTable(
                name: "ProspectSummary");
        }
    }
}
