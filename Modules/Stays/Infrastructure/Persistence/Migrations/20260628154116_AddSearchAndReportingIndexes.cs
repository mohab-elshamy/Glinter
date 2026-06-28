using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchAndReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_stays_Name_trgm"
                ON stays USING gin ("Name" gin_trgm_ops);
                CREATE INDEX "IX_stays_Description_trgm"
                ON stays USING gin ("Description" gin_trgm_ops);
                CREATE INDEX "IX_stays_Address_trgm"
                ON stays USING gin ("Address" gin_trgm_ops);
                CREATE INDEX "IX_stay_tags_Name_trgm"
                ON stay_tags USING gin ("Name" gin_trgm_ops);
                CREATE INDEX "IX_stay_tags_Name_lower"
                ON stay_tags (lower("Name"));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_stays_Adm3Gid_IsActive_CreatedAtUtc",
                table: "stays",
                columns: new[] { "Adm3Gid", "IsActive", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stays_IsActive_CreatedAtUtc",
                table: "stays",
                columns: new[] { "IsActive", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stays_IsActive_Currency_PricePerNight",
                table: "stays",
                columns: new[] { "IsActive", "Currency", "PricePerNight" });

            migrationBuilder.CreateIndex(
                name: "IX_stays_OwnerProfileId_CreatedAtUtc",
                table: "stays",
                columns: new[] { "OwnerProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_CreatedAtUtc_Status",
                table: "stay_bookings",
                columns: new[] { "CreatedAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_StayId_CheckInDate_CheckOutDate",
                table: "stay_bookings",
                columns: new[] { "StayId", "CheckInDate", "CheckOutDate" },
                filter: "\"Status\" <> 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "IX_stay_bookings_StayId_Status",
                table: "stay_bookings",
                columns: new[] { "StayId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_stay_tags_Name_lower";
                DROP INDEX IF EXISTS "IX_stay_tags_Name_trgm";
                DROP INDEX IF EXISTS "IX_stays_Address_trgm";
                DROP INDEX IF EXISTS "IX_stays_Description_trgm";
                DROP INDEX IF EXISTS "IX_stays_Name_trgm";
                """);

            migrationBuilder.DropIndex(
                name: "IX_stays_Adm3Gid_IsActive_CreatedAtUtc",
                table: "stays");

            migrationBuilder.DropIndex(
                name: "IX_stays_IsActive_CreatedAtUtc",
                table: "stays");

            migrationBuilder.DropIndex(
                name: "IX_stays_IsActive_Currency_PricePerNight",
                table: "stays");

            migrationBuilder.DropIndex(
                name: "IX_stays_OwnerProfileId_CreatedAtUtc",
                table: "stays");

            migrationBuilder.DropIndex(
                name: "IX_stay_bookings_CreatedAtUtc_Status",
                table: "stay_bookings");

            migrationBuilder.DropIndex(
                name: "IX_stay_bookings_StayId_CheckInDate_CheckOutDate",
                table: "stay_bookings");

            migrationBuilder.DropIndex(
                name: "IX_stay_bookings_StayId_Status",
                table: "stay_bookings");
        }
    }
}
