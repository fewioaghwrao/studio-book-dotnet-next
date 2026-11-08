using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Studiobook_backend.Infrastructure.Migrations
{
    /// <summary>
    /// B6/B7: Webhook の冪等化（Reservations.PaymentIntentId の一意制約）と
    /// レビューの重複防止（Reviews の RoomId + UserId 一意制約）。
    /// 既存の Reviews に同一 RoomId・UserId の重複があると一意制約の作成に失敗するため、
    /// 適用前に重複がないことを確認する（MIGRATION-NOTES.md 参照）。
    /// </summary>
    public partial class AddPaymentIntentIdAndReviewUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentIntentId",
                table: "Reservations",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PaymentIntentId",
                table: "Reservations",
                column: "PaymentIntentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_RoomId_UserId",
                table: "Reviews",
                columns: new[] { "RoomId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_RoomId_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PaymentIntentId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                table: "Reservations");
        }
    }
}
