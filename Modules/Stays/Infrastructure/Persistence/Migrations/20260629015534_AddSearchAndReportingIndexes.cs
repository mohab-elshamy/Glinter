using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations;

public partial class AddSearchAndReportingIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        EnsureTrigramExtension(migrationBuilder);

        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stays_Name_trgm",
            """
            CREATE INDEX CONCURRENTLY "IX_stays_Name_trgm"
            ON stays USING gin ("Name" gin_trgm_ops)
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stays_Description_trgm",
            """
            CREATE INDEX CONCURRENTLY "IX_stays_Description_trgm"
            ON stays USING gin ("Description" gin_trgm_ops)
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stays_Address_trgm",
            """
            CREATE INDEX CONCURRENTLY "IX_stays_Address_trgm"
            ON stays USING gin ("Address" gin_trgm_ops)
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stay_tags_Name_trgm",
            """
            CREATE INDEX CONCURRENTLY "IX_stay_tags_Name_trgm"
            ON stay_tags USING gin ("Name" gin_trgm_ops)
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stay_tags_Name_lower",
            """
            CREATE INDEX CONCURRENTLY "IX_stay_tags_Name_lower"
            ON stay_tags (lower("Name"))
            """);

        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stays_Adm3Gid_IsActive_CreatedAtUtc",
            """
            CREATE INDEX CONCURRENTLY "IX_stays_Adm3Gid_IsActive_CreatedAtUtc"
            ON stays ("Adm3Gid", "IsActive", "CreatedAtUtc")
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stays_IsActive_CreatedAtUtc",
            """
            CREATE INDEX CONCURRENTLY "IX_stays_IsActive_CreatedAtUtc"
            ON stays ("IsActive", "CreatedAtUtc")
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stays_IsActive_Currency_PricePerNight",
            """
            CREATE INDEX CONCURRENTLY "IX_stays_IsActive_Currency_PricePerNight"
            ON stays ("IsActive", "Currency", "PricePerNight")
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stays_OwnerProfileId_CreatedAtUtc",
            """
            CREATE INDEX CONCURRENTLY "IX_stays_OwnerProfileId_CreatedAtUtc"
            ON stays ("OwnerProfileId", "CreatedAtUtc")
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stay_bookings_CreatedAtUtc_Status",
            """
            CREATE INDEX CONCURRENTLY "IX_stay_bookings_CreatedAtUtc_Status"
            ON stay_bookings ("CreatedAtUtc", "Status")
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stay_bookings_StayId_CheckInDate_CheckOutDate",
            """
            CREATE INDEX CONCURRENTLY "IX_stay_bookings_StayId_CheckInDate_CheckOutDate"
            ON stay_bookings ("StayId", "CheckInDate", "CheckOutDate")
            WHERE "Status" <> 'Cancelled'
            """);
        RecreateConcurrentIndex(
            migrationBuilder,
            "IX_stay_bookings_StayId_Status",
            """
            CREATE INDEX CONCURRENTLY "IX_stay_bookings_StayId_Status"
            ON stay_bookings ("StayId", "Status")
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropConcurrentIndex(migrationBuilder, "IX_stay_bookings_StayId_Status");
        DropConcurrentIndex(
            migrationBuilder,
            "IX_stay_bookings_StayId_CheckInDate_CheckOutDate");
        DropConcurrentIndex(migrationBuilder, "IX_stay_bookings_CreatedAtUtc_Status");
        DropConcurrentIndex(migrationBuilder, "IX_stays_OwnerProfileId_CreatedAtUtc");
        DropConcurrentIndex(migrationBuilder, "IX_stays_IsActive_Currency_PricePerNight");
        DropConcurrentIndex(migrationBuilder, "IX_stays_IsActive_CreatedAtUtc");
        DropConcurrentIndex(migrationBuilder, "IX_stays_Adm3Gid_IsActive_CreatedAtUtc");
        DropConcurrentIndex(migrationBuilder, "IX_stay_tags_Name_lower");
        DropConcurrentIndex(migrationBuilder, "IX_stay_tags_Name_trgm");
        DropConcurrentIndex(migrationBuilder, "IX_stays_Address_trgm");
        DropConcurrentIndex(migrationBuilder, "IX_stays_Description_trgm");
        DropConcurrentIndex(migrationBuilder, "IX_stays_Name_trgm");
    }

    private static void EnsureTrigramExtension(MigrationBuilder migrationBuilder)
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
    }

    private static void RecreateConcurrentIndex(
        MigrationBuilder migrationBuilder,
        string indexName,
        string sql)
    {
        DropConcurrentIndex(migrationBuilder, indexName);
        migrationBuilder.Sql($"{sql};", suppressTransaction: true);
    }

    private static void DropConcurrentIndex(
        MigrationBuilder migrationBuilder,
        string indexName) =>
        migrationBuilder.Sql(
            $"""DROP INDEX CONCURRENTLY IF EXISTS "{indexName}";""",
            suppressTransaction: true);
}
