using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupAndReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_notifications_CreatedAtUtc",
                table: "notifications",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_ReadAtUtc",
                table: "notifications",
                column: "ReadAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_SentAtUtc",
                table: "chat_messages",
                column: "SentAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_CreatedAtUtc",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_ReadAtUtc",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_chat_messages_SentAtUtc",
                table: "chat_messages");
        }
    }
}
