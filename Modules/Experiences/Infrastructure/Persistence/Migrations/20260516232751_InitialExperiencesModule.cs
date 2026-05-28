using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialExperiencesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experience_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vibes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vibes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "experiences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    LocationName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PricePerPerson = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxGuests = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experiences_experience_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "experience_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "experience_availability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    BookedCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_availability", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_availability_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_reviews_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_tags_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_vibes",
                columns: table => new
                {
                    ExperienceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VibeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_vibes", x => new { x.ExperienceId, x.VibeId });
                    table.ForeignKey(
                        name: "FK_experience_vibes_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_experience_vibes_vibes_VibeId",
                        column: x => x.VibeId,
                        principalTable: "vibes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExperienceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experience_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_experience_bookings_experience_availability_AvailabilityId",
                        column: x => x.AvailabilityId,
                        principalTable: "experience_availability",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_experience_bookings_experiences_ExperienceId",
                        column: x => x.ExperienceId,
                        principalTable: "experiences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experience_availability_ExperienceId",
                table: "experience_availability",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_availability_IsActive",
                table: "experience_availability",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_experience_availability_StartTimeUtc",
                table: "experience_availability",
                column: "StartTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_AvailabilityId",
                table: "experience_bookings",
                column: "AvailabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_ExperienceId",
                table: "experience_bookings",
                column: "ExperienceId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_Status",
                table: "experience_bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_TravelerProfileId",
                table: "experience_bookings",
                column: "TravelerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_experience_categories_Name",
                table: "experience_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_ExperienceId_TravelerProfileId",
                table: "experience_reviews",
                columns: new[] { "ExperienceId", "TravelerProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_tags_ExperienceId_Name",
                table: "experience_tags",
                columns: new[] { "ExperienceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_vibes_VibeId",
                table: "experience_vibes",
                column: "VibeId");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_AreaId",
                table: "experiences",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_CategoryId",
                table: "experiences",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_IsActive",
                table: "experiences",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_experiences_ProviderProfileId",
                table: "experiences",
                column: "ProviderProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_vibes_Name",
                table: "vibes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experience_bookings");

            migrationBuilder.DropTable(
                name: "experience_reviews");

            migrationBuilder.DropTable(
                name: "experience_tags");

            migrationBuilder.DropTable(
                name: "experience_vibes");

            migrationBuilder.DropTable(
                name: "experience_availability");

            migrationBuilder.DropTable(
                name: "vibes");

            migrationBuilder.DropTable(
                name: "experiences");

            migrationBuilder.DropTable(
                name: "experience_categories");
        }
    }
}
