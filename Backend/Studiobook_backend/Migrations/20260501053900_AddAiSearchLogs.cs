using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Studiobook_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSearchLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiSearchLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSearchLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiSearchLogs_CreatedAtUtc",
                table: "AiSearchLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiSearchLogs_IpAddress",
                table: "AiSearchLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AiSearchLogs_Succeeded",
                table: "AiSearchLogs",
                column: "Succeeded");

            migrationBuilder.CreateIndex(
                name: "IX_AiSearchLogs_UserId",
                table: "AiSearchLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiSearchLogs");
        }
    }
}
