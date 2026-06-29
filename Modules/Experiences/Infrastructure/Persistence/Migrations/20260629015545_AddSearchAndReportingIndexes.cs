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
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm'
                    ) THEN
                        RAISE EXCEPTION
                            'Required extension pg_trgm is not installed. Run docs/postgres-prerequisites.sql with a privileged database role before migrations.';
                    END IF;
                END
                $$;
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experiences_Title_trgm",
                """
                CREATE INDEX CONCURRENTLY "IX_experiences_Title_trgm"
                ON experiences USING gin ("Title" gin_trgm_ops)
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experiences_Description_trgm",
                """
                CREATE INDEX CONCURRENTLY "IX_experiences_Description_trgm"
                ON experiences USING gin ("Description" gin_trgm_ops)
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experiences_LocationName_trgm",
                """
                CREATE INDEX CONCURRENTLY "IX_experiences_LocationName_trgm"
                ON experiences USING gin ("LocationName" gin_trgm_ops)
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experience_tags_Name_trgm",
                """
                CREATE INDEX CONCURRENTLY "IX_experience_tags_Name_trgm"
                ON experience_tags USING gin ("Name" gin_trgm_ops)
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experience_tags_Name_lower",
                """
                CREATE INDEX CONCURRENTLY "IX_experience_tags_Name_lower"
                ON experience_tags (lower("Name"))
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experiences_Adm3Gid_IsActive_ApprovalStatus",
                """
                CREATE INDEX CONCURRENTLY "IX_experiences_Adm3Gid_IsActive_ApprovalStatus"
                ON experiences ("Adm3Gid", "IsActive", "ApprovalStatus")
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experiences_CategoryId_IsActive_ApprovalStatus",
                """
                CREATE INDEX CONCURRENTLY "IX_experiences_CategoryId_IsActive_ApprovalStatus"
                ON experiences ("CategoryId", "IsActive", "ApprovalStatus")
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experiences_IsActive_ApprovalStatus_CreatedAtUtc",
                """
                CREATE INDEX CONCURRENTLY "IX_experiences_IsActive_ApprovalStatus_CreatedAtUtc"
                ON experiences ("IsActive", "ApprovalStatus", "CreatedAtUtc")
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experiences_ProviderProfileId_CreatedAtUtc",
                """
                CREATE INDEX CONCURRENTLY "IX_experiences_ProviderProfileId_CreatedAtUtc"
                ON experiences ("ProviderProfileId", "CreatedAtUtc")
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experience_bookings_CreatedAtUtc_Status",
                """
                CREATE INDEX CONCURRENTLY "IX_experience_bookings_CreatedAtUtc_Status"
                ON experience_bookings ("CreatedAtUtc", "Status")
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experience_bookings_ExperienceId_Status",
                """
                CREATE INDEX CONCURRENTLY "IX_experience_bookings_ExperienceId_Status"
                ON experience_bookings ("ExperienceId", "Status")
                """);
            RecreateConcurrentIndex(
                migrationBuilder,
                "IX_experience_availability_ExperienceId_IsActive_StartTimeUtc_~",
                """
                CREATE INDEX CONCURRENTLY "IX_experience_availability_ExperienceId_IsActive_StartTimeUtc_~"
                ON experience_availability
                    ("ExperienceId", "IsActive", "StartTimeUtc", "EndTimeUtc")
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experience_availability_ExperienceId_IsActive_StartTimeUtc_~");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experience_bookings_ExperienceId_Status");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experience_bookings_CreatedAtUtc_Status");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experiences_ProviderProfileId_CreatedAtUtc");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experiences_IsActive_ApprovalStatus_CreatedAtUtc");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experiences_CategoryId_IsActive_ApprovalStatus");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experiences_Adm3Gid_IsActive_ApprovalStatus");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experience_tags_Name_lower");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experience_tags_Name_trgm");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experiences_LocationName_trgm");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experiences_Description_trgm");
            DropConcurrentIndex(
                migrationBuilder,
                "IX_experiences_Title_trgm");
        }

        private static void DropConcurrentIndex(
            MigrationBuilder migrationBuilder,
            string indexName) =>
            migrationBuilder.Sql(
                $"""DROP INDEX CONCURRENTLY IF EXISTS "{indexName}";""",
                suppressTransaction: true);

        private static void RecreateConcurrentIndex(
            MigrationBuilder migrationBuilder,
            string indexName,
            string sql)
        {
            DropConcurrentIndex(migrationBuilder, indexName);
            migrationBuilder.Sql($"{sql};", suppressTransaction: true);
        }
    }
}
