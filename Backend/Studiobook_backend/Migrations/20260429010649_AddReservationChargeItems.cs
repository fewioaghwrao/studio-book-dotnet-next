using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Studiobook_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationChargeItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationChargeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SliceAmount = table.Column<int>(type: "int", nullable: false),
                    SliceStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SliceEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnitRatePerHour = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationChargeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationChargeItems_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationChargeItems_ReservationId",
                table: "ReservationChargeItems",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationChargeItems_SliceStart",
                table: "ReservationChargeItems",
                column: "SliceStart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationChargeItems");
        }
    }
}
