using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStayReviewBookingEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                schema: "stays",
                table: "stay_reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stay_reviews_BookingId",
                schema: "stays",
                table: "stay_reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stay_reviews_stay_bookings_BookingId",
                schema: "stays",
                table: "stay_reviews",
                column: "BookingId",
                principalSchema: "stays",
                principalTable: "stay_bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stay_reviews_stay_bookings_BookingId",
                schema: "stays",
                table: "stay_reviews");

            migrationBuilder.DropIndex(
                name: "IX_stay_reviews_BookingId",
                schema: "stays",
                table: "stay_reviews");

            migrationBuilder.DropColumn(
                name: "BookingId",
                schema: "stays",
                table: "stay_reviews");
        }
    }
}
