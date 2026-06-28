using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
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
                CREATE INDEX "IX_experiences_Title_trgm"
                ON experiences USING gin ("Title" gin_trgm_ops);
                CREATE INDEX "IX_experiences_Description_trgm"
                ON experiences USING gin ("Description" gin_trgm_ops);
                CREATE INDEX "IX_experiences_LocationName_trgm"
                ON experiences USING gin ("LocationName" gin_trgm_ops);
                CREATE INDEX "IX_experience_tags_Name_trgm"
                ON experience_tags USING gin ("Name" gin_trgm_ops);
                CREATE INDEX "IX_experience_tags_Name_lower"
                ON experience_tags (lower("Name"));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_experiences_Adm3Gid_IsActive_ApprovalStatus",
                table: "experiences",
                columns: new[] { "Adm3Gid", "IsActive", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_experiences_CategoryId_IsActive_ApprovalStatus",
                table: "experiences",
                columns: new[] { "CategoryId", "IsActive", "ApprovalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_experiences_IsActive_ApprovalStatus_CreatedAtUtc",
                table: "experiences",
                columns: new[] { "IsActive", "ApprovalStatus", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_experiences_ProviderProfileId_CreatedAtUtc",
                table: "experiences",
                columns: new[] { "ProviderProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_CreatedAtUtc_Status",
                table: "experience_bookings",
                columns: new[] { "CreatedAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_experience_bookings_ExperienceId_Status",
                table: "experience_bookings",
                columns: new[] { "ExperienceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_experience_availability_ExperienceId_IsActive_StartTimeUtc_~",
                table: "experience_availability",
                columns: new[] { "ExperienceId", "IsActive", "StartTimeUtc", "EndTimeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_experience_tags_Name_lower";
                DROP INDEX IF EXISTS "IX_experience_tags_Name_trgm";
                DROP INDEX IF EXISTS "IX_experiences_LocationName_trgm";
                DROP INDEX IF EXISTS "IX_experiences_Description_trgm";
                DROP INDEX IF EXISTS "IX_experiences_Title_trgm";
                """);

            migrationBuilder.DropIndex(
                name: "IX_experiences_Adm3Gid_IsActive_ApprovalStatus",
                table: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experiences_CategoryId_IsActive_ApprovalStatus",
                table: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experiences_IsActive_ApprovalStatus_CreatedAtUtc",
                table: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experiences_ProviderProfileId_CreatedAtUtc",
                table: "experiences");

            migrationBuilder.DropIndex(
                name: "IX_experience_bookings_CreatedAtUtc_Status",
                table: "experience_bookings");

            migrationBuilder.DropIndex(
                name: "IX_experience_bookings_ExperienceId_Status",
                table: "experience_bookings");

            migrationBuilder.DropIndex(
                name: "IX_experience_availability_ExperienceId_IsActive_StartTimeUtc_~",
                table: "experience_availability");
        }
    }
}
