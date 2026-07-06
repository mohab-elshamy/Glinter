using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ExperiencesDbContext))]
    [Migration("20260706090000_SplitExperiencePriceRange")]
    public partial class SplitExperiencePriceRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experiences'
                          AND column_name = 'PriceRangeMin'
                    ) THEN
                        ALTER TABLE experiences.experiences
                        ADD COLUMN "PriceRangeMin" integer;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experiences'
                          AND column_name = 'PriceRangeMax'
                    ) THEN
                        ALTER TABLE experiences.experiences
                        ADD COLUMN "PriceRangeMax" integer;
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experiences'
                          AND column_name = 'PriceRange'
                    ) THEN
                        UPDATE experiences.experiences
                        SET "PriceRangeMin" = 0,
                            "PriceRangeMax" = 0
                        WHERE lower(trim("PriceRange")) = 'free';

                        WITH price_numbers AS (
                            SELECT
                                e."Id",
                                array_agg((matches.match)[1]::integer ORDER BY matches.ordinality) AS numbers
                            FROM experiences.experiences e
                            CROSS JOIN LATERAL regexp_matches(
                                replace(coalesce(e."PriceRange", ''), ',', ''),
                                '\d+',
                                'g') WITH ORDINALITY AS matches(match, ordinality)
                            WHERE e."PriceRange" IS NOT NULL
                            GROUP BY e."Id"
                        )
                        UPDATE experiences.experiences e
                        SET
                            "PriceRangeMin" = LEAST(price_numbers.numbers[1], COALESCE(price_numbers.numbers[2], price_numbers.numbers[1])),
                            "PriceRangeMax" = GREATEST(price_numbers.numbers[1], COALESCE(price_numbers.numbers[2], price_numbers.numbers[1]))
                        FROM price_numbers
                        WHERE e."Id" = price_numbers."Id";

                        ALTER TABLE experiences.experiences
                        DROP COLUMN "PriceRange";
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experiences'
                          AND column_name = 'PriceRange'
                    ) THEN
                        ALTER TABLE experiences.experiences
                        ADD COLUMN "PriceRange" character varying(100);
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experiences'
                          AND column_name = 'PriceRangeMin'
                    ) THEN
                        UPDATE experiences.experiences
                        SET "PriceRange" = CASE
                            WHEN "PriceRangeMin" IS NULL AND "PriceRangeMax" IS NULL THEN NULL
                            WHEN "PriceRangeMin" = 0 AND COALESCE("PriceRangeMax", 0) = 0 THEN 'Free'
                            WHEN COALESCE("PriceRangeMin", "PriceRangeMax") = COALESCE("PriceRangeMax", "PriceRangeMin")
                                THEN '$' || COALESCE("PriceRangeMin", "PriceRangeMax")::text
                            ELSE '$' || LEAST("PriceRangeMin", "PriceRangeMax")::text || '-$' || GREATEST("PriceRangeMin", "PriceRangeMax")::text
                        END;

                        ALTER TABLE experiences.experiences
                        DROP COLUMN "PriceRangeMin";
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experiences'
                          AND column_name = 'PriceRangeMax'
                    ) THEN
                        ALTER TABLE experiences.experiences
                        DROP COLUMN "PriceRangeMax";
                    END IF;
                END $$;
                """);
        }
    }
}
