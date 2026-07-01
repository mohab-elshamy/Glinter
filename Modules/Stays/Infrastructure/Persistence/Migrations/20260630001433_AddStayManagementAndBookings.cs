using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStayManagementAndBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "stays",
                table: "stays",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "stay_bookings",
                schema: "stays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    TravelerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GuestCount = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stay_bookings_stays_StayId",
                        column: x => x.StayId,
                        principalSchema: "stays",
                        principalTable: "stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stays_IsActive",
                schema: "stays",
                table: "stays",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_CreatedByUserId",
                schema: "stays",
                table: "stay_bookings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_StayId",
                schema: "stays",
                table: "stay_bookings",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_StayId_CheckInDate_CheckOutDate",
                schema: "stays",
                table: "stay_bookings",
                columns: new[] { "StayId", "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_TravelerProfileId",
                schema: "stays",
                table: "stay_bookings",
                column: "TravelerProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stay_bookings",
                schema: "stays");

            migrationBuilder.DropIndex(
                name: "IX_stays_IsActive",
                schema: "stays",
                table: "stays");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "stays",
                table: "stays");
        }
    }
}
