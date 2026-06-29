using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations;

[DbContext(typeof(StaysDbContext))]
[Migration("20260627000000_AllowCancelledStayRebooking")]
public partial class AllowCancelledStayRebooking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_stay_bookings_StayId_TravelerProfileId_CheckInDate_CheckOut~",
            table: "stay_bookings");

        migrationBuilder.CreateIndex(
            name: "IX_stay_bookings_StayId_TravelerProfileId_CheckInDate_CheckOut~",
            table: "stay_bookings",
            columns: new[] { "StayId", "TravelerProfileId", "CheckInDate", "CheckOutDate" },
            unique: true,
            filter: "\"Status\" <> 'Cancelled'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_stay_bookings_StayId_TravelerProfileId_CheckInDate_CheckOut~",
            table: "stay_bookings");

        migrationBuilder.CreateIndex(
            name: "IX_stay_bookings_StayId_TravelerProfileId_CheckInDate_CheckOut~",
            table: "stay_bookings",
            columns: new[] { "StayId", "TravelerProfileId", "CheckInDate", "CheckOutDate" },
            unique: true);
    }
}
