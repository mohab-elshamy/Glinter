using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Experiences.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitExperienceAmenityNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS experiences."IX_experience_amenities_ExperienceId_Name";

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'NameAr'
                    ) THEN
                        ALTER TABLE experiences.experience_amenities
                        ADD COLUMN "NameAr" character varying(250);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'NameEn'
                    ) THEN
                        ALTER TABLE experiences.experience_amenities
                        ADD COLUMN "NameEn" character varying(250);
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'Name'
                    ) THEN
                        UPDATE experiences.experience_amenities
                        SET
                            "NameAr" = CASE
                                WHEN "Name" ~ '[؀-ۿ]' THEN COALESCE("NameAr", "Name")
                                ELSE "NameAr"
                            END,
                            "NameEn" = CASE
                                WHEN "Name" ~ '[؀-ۿ]' THEN "NameEn"
                                ELSE COALESCE("NameEn", "Name")
                            END
                        WHERE "Name" IS NOT NULL;

                        ALTER TABLE experiences.experience_amenities
                        DROP COLUMN "Name";
                    END IF;
                END $$;

                UPDATE experiences.experience_amenities
                SET "NameEn" = CASE "NameAr"
                    WHEN 'مرحاض' THEN 'Toilet'
                    WHEN 'اتصال Wi-Fi' THEN 'Wi-Fi access'
                    WHEN 'Wi-Fi' THEN 'Wi-Fi'
                    WHEN 'دورة مياه للجنسين' THEN 'Gender-neutral restroom'
                    WHEN 'بار داخل المكان' THEN 'On-site bar'
                    WHEN 'حمّامات عامة' THEN 'Public restrooms'
                    WHEN 'طاولات للنزهات' THEN 'Picnic tables'
                    WHEN 'أراجيح' THEN 'Swings'
                    WHEN 'مطعم' THEN 'Restaurant'
                    WHEN 'شواية' THEN 'Grill'
                    WHEN 'زحاليق' THEN 'Slides'
                    WHEN 'ممرات للدرّاجات' THEN 'Bicycle paths'
                    WHEN 'منطقة تزلّج على الألواح' THEN 'Skateboarding area'
                    WHEN 'ملعب كرة سلة' THEN 'Basketball court'
                    WHEN 'ملعب لكرة الطائرة' THEN 'Volleyball court'
                    WHEN 'مسبح' THEN 'Pool'
                    WHEN 'يتوفّر ملعب تنس' THEN 'Tennis court available'
                    WHEN 'ميكانيكي' THEN 'Mechanic'
                    WHEN 'تخزين الأمتعة' THEN 'Luggage storage'
                    WHEN 'حمام سباحة' THEN 'Swimming pool'
                    WHEN 'حوض استحمام ساخن' THEN 'Hot tub'
                    WHEN 'خدمات الحفلات' THEN 'Event services'
                    WHEN 'ماكينة صراف آلي' THEN 'ATM'
                    WHEN 'مساحة خارجية' THEN 'Outdoor space'
                    WHEN 'مضخة هواء' THEN 'Air pump'
                    WHEN 'يسمح باصطحاب الحيوانات الأليفة' THEN 'Pet-friendly'
                    ELSE "NameEn"
                END;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_experience_amenities_ExperienceId_NameAr_NameEn"
                ON experiences.experience_amenities ("ExperienceId", "NameAr", "NameEn");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS experiences."IX_experience_amenities_ExperienceId_NameAr_NameEn";

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'Name'
                    ) THEN
                        ALTER TABLE experiences.experience_amenities
                        ADD COLUMN "Name" character varying(250) NOT NULL DEFAULT '';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'NameEn'
                    ) THEN
                        UPDATE experiences.experience_amenities
                        SET "Name" = COALESCE("NameEn", "NameAr", "Name");
                    ELSIF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'NameAr'
                    ) THEN
                        UPDATE experiences.experience_amenities
                        SET "Name" = COALESCE("NameAr", "Name");
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'NameAr'
                    ) THEN
                        ALTER TABLE experiences.experience_amenities
                        DROP COLUMN "NameAr";
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'experiences'
                          AND table_name = 'experience_amenities'
                          AND column_name = 'NameEn'
                    ) THEN
                        ALTER TABLE experiences.experience_amenities
                        DROP COLUMN "NameEn";
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_experience_amenities_ExperienceId_Name"
                ON experiences.experience_amenities ("ExperienceId", "Name");
                """);
        }
    }
}
