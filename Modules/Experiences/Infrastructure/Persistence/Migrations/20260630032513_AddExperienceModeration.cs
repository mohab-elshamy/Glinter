using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAtUtc",
                schema: "experiences",
                table: "experiences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModeratedByUserId",
                schema: "experiences",
                table: "experiences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationNotes",
                schema: "experiences",
                table: "experiences",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationStatus",
                schema: "experiences",
                table: "experiences",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_ModerationStatus",
                schema: "experiences",
                table: "experiences",
                column: "ModerationStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_experiences_ModerationStatus",
                schema: "experiences",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "ModeratedAtUtc",
                schema: "experiences",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                schema: "experiences",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "ModerationNotes",
                schema: "experiences",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                schema: "experiences",
                table: "experiences");
        }
    }
}
