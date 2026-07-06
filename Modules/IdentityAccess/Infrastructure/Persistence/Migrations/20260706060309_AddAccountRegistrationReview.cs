using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.IdentityAccess.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountRegistrationReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountReviewNotes",
                table: "users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountReviewStatus",
                table: "users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "NotRequired");

            migrationBuilder.AddColumn<DateTime>(
                name: "AccountReviewedAtUtc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountReviewedByUserId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityDocumentContentType",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityDocumentFileName",
                table: "users",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityDocumentUrl",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE users AS u
                SET "AccountReviewStatus" = 'Approved'
                FROM user_roles AS ur
                INNER JOIN roles AS r ON r."Id" = ur."RoleId"
                WHERE u."Id" = ur."UserId"
                  AND r."Name" IN ('LocalBuddy', 'HotelOwner', 'ExperienceProvider');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_AccountReviewStatus",
                table: "users",
                column: "AccountReviewStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_AccountReviewStatus",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AccountReviewNotes",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AccountReviewStatus",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AccountReviewedAtUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AccountReviewedByUserId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IdentityDocumentContentType",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IdentityDocumentFileName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IdentityDocumentUrl",
                table: "users");
        }
    }
}
