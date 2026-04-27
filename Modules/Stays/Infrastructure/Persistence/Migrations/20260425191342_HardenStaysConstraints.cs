using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenStaysConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stay_reviews_StayId",
                table: "stay_reviews");

            migrationBuilder.DropIndex(
                name: "IX_stay_bookings_StayId",
                table: "stay_bookings");

            migrationBuilder.CreateIndex(
                name: "IX_stays_OwnerProfileId_Name_Address",
                table: "stays",
                columns: new[] { "OwnerProfileId", "Name", "Address" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_StayId_TravelerProfileId",
                table: "stay_reviews",
                columns: new[] { "StayId", "TravelerProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_StayId_TravelerProfileId_CheckInDate_CheckOut~",
                table: "stay_bookings",
                columns: new[] { "StayId", "TravelerProfileId", "CheckInDate", "CheckOutDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stays_OwnerProfileId_Name_Address",
                table: "stays");

            migrationBuilder.DropIndex(
                name: "IX_stay_reviews_StayId_TravelerProfileId",
                table: "stay_reviews");

            migrationBuilder.DropIndex(
                name: "IX_stay_bookings_StayId_TravelerProfileId_CheckInDate_CheckOut~",
                table: "stay_bookings");

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_StayId",
                table: "stay_reviews",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_StayId",
                table: "stay_bookings",
                column: "StayId");
        }
    }
}
