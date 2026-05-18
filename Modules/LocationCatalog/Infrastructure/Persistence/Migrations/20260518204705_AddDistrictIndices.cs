using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.LocationCatalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "district_indices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistrictId = table.Column<Guid>(type: "uuid", nullable: false),
                    SafetyScore = table.Column<double>(type: "double precision", nullable: false),
                    SafetyLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SafetyExplanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PriceScore = table.Column<double>(type: "double precision", nullable: true),
                    PriceLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PriceExplanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ServicesScore = table.Column<double>(type: "double precision", nullable: true),
                    ServicesLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ServicesExplanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ComputedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_district_indices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_district_indices_districts_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "districts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_district_indices_DistrictId",
                table: "district_indices",
                column: "DistrictId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "district_indices");
        }
    }
}
