using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupAndReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_users_CreatedAtUtc",
                table: "users",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_users_IsActive",
                table: "users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_revoked_tokens_ExpiresAtUtc",
                table: "revoked_tokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAtUtc",
                table: "refresh_tokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_RevokedAtUtc",
                table: "refresh_tokens",
                column: "RevokedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_mfa_challenges_ConsumedAtUtc",
                table: "mfa_challenges",
                column: "ConsumedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_mfa_challenges_ExpiresAtUtc",
                table: "mfa_challenges",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_CreatedAtUtc",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_IsActive",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_revoked_tokens_ExpiresAtUtc",
                table: "revoked_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_ExpiresAtUtc",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_RevokedAtUtc",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_mfa_challenges_ConsumedAtUtc",
                table: "mfa_challenges");

            migrationBuilder.DropIndex(
                name: "IX_mfa_challenges_ExpiresAtUtc",
                table: "mfa_challenges");
        }
    }
}
