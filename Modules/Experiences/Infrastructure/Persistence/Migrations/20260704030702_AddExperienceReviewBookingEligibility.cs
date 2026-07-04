using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExperienceReviewBookingEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                schema: "experiences",
                table: "experience_reviews",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_experience_reviews_BookingId",
                schema: "experiences",
                table: "experience_reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_experience_reviews_experience_bookings_BookingId",
                schema: "experiences",
                table: "experience_reviews",
                column: "BookingId",
                principalSchema: "experiences",
                principalTable: "experience_bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_experience_reviews_experience_bookings_BookingId",
                schema: "experiences",
                table: "experience_reviews");

            migrationBuilder.DropIndex(
                name: "IX_experience_reviews_BookingId",
                schema: "experiences",
                table: "experience_reviews");

            migrationBuilder.DropColumn(
                name: "BookingId",
                schema: "experiences",
                table: "experience_reviews");
        }
    }
}
