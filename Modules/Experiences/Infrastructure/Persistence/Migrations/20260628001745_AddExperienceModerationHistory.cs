using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceModerationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experience_moderation_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_moderation_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_moderation_events_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experience_moderation_events_ActorUserId_CreatedAtUtc",
                table: "experience_moderation_events",
                columns: new[] { "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_experience_moderation_events_ExperienceId_CreatedAtUtc",
                table: "experience_moderation_events",
                columns: new[] { "ExperienceId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experience_moderation_events");
        }
    }
}
