using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Studiobook_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Weekday = table.Column<int>(type: "int", nullable: true),
                    StartHour = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndHour = table.Column<TimeOnly>(type: "time", nullable: true),
                    Multiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FlatFee = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceRules_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_RoomId_RuleType_Weekday",
                table: "PriceRules",
                columns: new[] { "RoomId", "RuleType", "Weekday" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceRules");
        }
    }
}
