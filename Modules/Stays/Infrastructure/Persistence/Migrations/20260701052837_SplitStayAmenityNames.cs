using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitStayAmenityNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS stays."IX_stay_amenities_StayId_Name";

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'NameAr'
                    ) THEN
                        ALTER TABLE stays.stay_amenities
                        ADD COLUMN "NameAr" character varying(250);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'NameEn'
                    ) THEN
                        ALTER TABLE stays.stay_amenities
                        ADD COLUMN "NameEn" character varying(250);
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'Name'
                    ) THEN
                        UPDATE stays.stay_amenities
                        SET "NameEn" = COALESCE("NameEn", "Name")
                        WHERE "Name" IS NOT NULL;

                        ALTER TABLE stays.stay_amenities
                        DROP COLUMN "Name";
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_stay_amenities_StayId_NameAr_NameEn"
                ON stays.stay_amenities ("StayId", "NameAr", "NameEn");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS stays."IX_stay_amenities_StayId_NameAr_NameEn";

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'Name'
                    ) THEN
                        ALTER TABLE stays.stay_amenities
                        ADD COLUMN "Name" character varying(250) NOT NULL DEFAULT '';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'NameEn'
                    ) THEN
                        UPDATE stays.stay_amenities
                        SET "Name" = COALESCE("NameEn", "NameAr", "Name");
                    ELSIF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'NameAr'
                    ) THEN
                        UPDATE stays.stay_amenities
                        SET "Name" = COALESCE("NameAr", "Name");
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'NameAr'
                    ) THEN
                        ALTER TABLE stays.stay_amenities
                        DROP COLUMN "NameAr";
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'stays'
                          AND table_name = 'stay_amenities'
                          AND column_name = 'NameEn'
                    ) THEN
                        ALTER TABLE stays.stay_amenities
                        DROP COLUMN "NameEn";
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_stay_amenities_StayId_Name"
                ON stays.stay_amenities ("StayId", "Name");
                """);
        }
    }
}
