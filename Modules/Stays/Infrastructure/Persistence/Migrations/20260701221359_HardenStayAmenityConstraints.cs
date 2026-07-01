using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenStayAmenityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stay_amenities_StayId_NameAr_NameEn",
                schema: "stays",
                table: "stay_amenities");

            migrationBuilder.CreateIndex(
                name: "IX_stay_amenities_StayId_NameAr_NameEn",
                schema: "stays",
                table: "stay_amenities",
                columns: new[] { "StayId", "NameAr", "NameEn" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_stay_amenities_has_name",
                schema: "stays",
                table: "stay_amenities",
                sql: "NULLIF(BTRIM(\"NameAr\"), '') IS NOT NULL\r\nOR NULLIF(BTRIM(\"NameEn\"), '') IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stay_amenities_StayId_NameAr_NameEn",
                schema: "stays",
                table: "stay_amenities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_stay_amenities_has_name",
                schema: "stays",
                table: "stay_amenities");

            migrationBuilder.CreateIndex(
                name: "IX_stay_amenities_StayId_NameAr_NameEn",
                schema: "stays",
                table: "stay_amenities",
                columns: new[] { "StayId", "NameAr", "NameEn" },
                unique: true);
        }
    }
}
