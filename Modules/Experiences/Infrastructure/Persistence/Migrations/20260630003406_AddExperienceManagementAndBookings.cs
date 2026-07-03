using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceManagementAndBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "experiences",
                table: "experiences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                schema: "experiences",
                table: "experience_reviews",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "experiences",
                table: "experience_reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "experience_availability",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    PricePerPerson = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_availability", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_availability_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_bookings",
                schema: "experiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<int>(type: "integer", nullable: false),
                    AvailabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GuestsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_bookings_experience_availability_AvailabilityId",
                        column: x => x.AvailabilityId,
                        principalSchema: "experiences",
                        principalTable: "experience_availability",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_experience_bookings_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalSchema: "experiences",
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experiences_IsActive",
                schema: "experiences",
                table: "experiences",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_CreatedByUserId",
                schema: "experiences",
                table: "experience_reviews",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_availability_ExperienceId",
                schema: "experiences",
                table: "experience_availability",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_availability_ExperienceId_StartTimeUtc_EndTimeUtc",
                schema: "experiences",
                table: "experience_availability",
                columns: new[] { "ExperienceId", "StartTimeUtc", "EndTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_AvailabilityId",
                schema: "experiences",
                table: "experience_bookings",
                column: "AvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_CreatedByUserId",
                schema: "experiences",
                table: "experience_bookings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_ExperienceId",
                schema: "experiences",
                table: "experience_bookings",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_TravelerProfileId",
                schema: "experiences",
                table: "experience_bookings",
                column: "TravelerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experience_bookings",
                schema: "experiences");

            migrationBuilder.DropTable(
                name: "experience_availability",
                schema: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experiences_IsActive",
                schema: "experiences",
                table: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experience_reviews_CreatedByUserId",
                schema: "experiences",
                table: "experience_reviews");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "experiences",
                table: "experiences");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "experiences",
                table: "experience_reviews");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "experiences",
                table: "experience_reviews");
        }
    }
}
