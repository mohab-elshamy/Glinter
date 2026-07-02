using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStayFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stay_favorites",
                schema: "stays",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stay_favorites", x => new { x.UserId, x.StayId });
                    table.ForeignKey(
                        name: "FK_stay_favorites_stays_StayId",
                        column: x => x.StayId,
                        principalSchema: "stays",
                        principalTable: "stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stay_favorites_StayId",
                schema: "stays",
                table: "stay_favorites",
                column: "StayId");

            migrationBuilder.CreateIndex(
                name: "IX_stay_favorites_UserId_CreatedAtUtc",
                schema: "stays",
                table: "stay_favorites",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stay_favorites",
                schema: "stays");
        }
    }
}
