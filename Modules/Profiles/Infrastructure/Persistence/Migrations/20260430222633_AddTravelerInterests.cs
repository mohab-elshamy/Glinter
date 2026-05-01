using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Profiles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelerInterests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "traveler_interests",
                columns: table => new
                {
                    TravelerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traveler_interests", x => new { x.TravelerProfileId, x.InterestId });
                    table.ForeignKey(
                        name: "FK_traveler_interests_interests_InterestId",
                        column: x => x.InterestId,
                        principalTable: "interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_traveler_interests_traveler_profiles_TravelerProfileId",
                        column: x => x.TravelerProfileId,
                        principalTable: "traveler_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_traveler_interests_InterestId",
                table: "traveler_interests",
                column: "InterestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "traveler_interests");
        }
    }
}
