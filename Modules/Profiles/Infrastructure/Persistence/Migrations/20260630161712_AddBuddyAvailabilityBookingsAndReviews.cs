using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuddyAvailabilityBookingsAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buddy_availability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalBuddyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buddy_availability", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "buddy_bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalBuddyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buddy_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_buddy_bookings_buddy_availability_AvailabilityId",
                        column: x => x.AvailabilityId,
                        principalTable: "buddy_availability",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "buddy_reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalBuddyUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    ReviewText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buddy_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_buddy_reviews_buddy_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "buddy_bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_buddy_availability_LocalBuddyUserId_StartTimeUtc_EndTimeUtc",
                table: "buddy_availability",
                columns: new[] { "LocalBuddyUserId", "StartTimeUtc", "EndTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_buddy_bookings_AvailabilityId",
                table: "buddy_bookings",
                column: "AvailabilityId",
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Accepted')");

            migrationBuilder.CreateIndex(
                name: "IX_buddy_bookings_LocalBuddyUserId_Status",
                table: "buddy_bookings",
                columns: new[] { "LocalBuddyUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_buddy_bookings_TravelerUserId_Status",
                table: "buddy_bookings",
                columns: new[] { "TravelerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_buddy_reviews_BookingId",
                table: "buddy_reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_buddy_reviews_LocalBuddyUserId_CreatedAtUtc",
                table: "buddy_reviews",
                columns: new[] { "LocalBuddyUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buddy_reviews");

            migrationBuilder.DropTable(
                name: "buddy_bookings");

            migrationBuilder.DropTable(
                name: "buddy_availability");
        }
    }
}
