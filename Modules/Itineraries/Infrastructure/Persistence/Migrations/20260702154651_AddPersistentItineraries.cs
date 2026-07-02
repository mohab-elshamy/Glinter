using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Itineraries.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentItineraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "itineraries");

            migrationBuilder.CreateTable(
                name: "saved_itineraries",
                schema: "itineraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Destination = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    Adm0Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm1Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm2Gid = table.Column<int>(type: "integer", nullable: true),
                    Adm3Gid = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    EstimatedTotalCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_itineraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "saved_itinerary_items",
                schema: "itineraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItineraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: true),
                    NameSnapshot = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Explanation = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    TravelModeFromPrevious = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    DistanceKmFromPrevious = table.Column<double>(type: "double precision", nullable: true),
                    TravelDurationMinutesFromPrevious = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_itinerary_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_itinerary_items_saved_itineraries_ItineraryId",
                        column: x => x.ItineraryId,
                        principalSchema: "itineraries",
                        principalTable: "saved_itineraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_saved_itineraries_UserId_UpdatedAtUtc",
                schema: "itineraries",
                table: "saved_itineraries",
                columns: new[] { "UserId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_saved_itinerary_items_ItineraryId_DayNumber_SortOrder",
                schema: "itineraries",
                table: "saved_itinerary_items",
                columns: new[] { "ItineraryId", "DayNumber", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_itinerary_items",
                schema: "itineraries");

            migrationBuilder.DropTable(
                name: "saved_itineraries",
                schema: "itineraries");
        }
    }
}
