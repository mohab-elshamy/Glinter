using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glinter.Modules.Stays.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillStayAmenityEnglishNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE stays.stay_amenities
                SET
                    "NameAr" = CASE
                        WHEN "NameAr" IS NULL AND "NameEn" ~ '[؀-ۿ]' THEN "NameEn"
                        ELSE "NameAr"
                    END,
                    "NameEn" = CASE
                        WHEN "NameEn" ~ '[؀-ۿ]' THEN NULL
                        ELSE "NameEn"
                    END
                WHERE "NameEn" ~ '[؀-ۿ]';

                UPDATE stays.stay_amenities
                SET "NameEn" = CASE "NameAr"
                    WHEN 'خدمة غسيل' THEN 'Laundry service'
                    WHEN 'مكيّف هواء' THEN 'Air conditioning'
                    WHEN 'خدمة غرف' THEN 'Room service'
                    WHEN 'اتصال Wi-Fi مجاني' THEN 'Free Wi-Fi'
                    WHEN 'مطعم' THEN 'Restaurant'
                    WHEN 'حافلة للمطار' THEN 'Airport shuttle'
                    WHEN 'مناسب للأطفال' THEN 'Kid-friendly'
                    WHEN 'مُناسب لذوي الاحتياجات الخاصة' THEN 'Accessible'
                    WHEN 'موقف سيارات مجاني' THEN 'Free parking'
                    WHEN 'إفطار مجاني' THEN 'Free breakfast'
                    WHEN 'Wi-Fi' THEN 'Wi-Fi'
                    WHEN 'حمام سباحة خارجي' THEN 'Outdoor pool'
                    WHEN 'صالة رياضة' THEN 'Fitness center'
                    WHEN 'بار' THEN 'Bar'
                    WHEN 'مطابخ في بعض الغرف' THEN 'Kitchen in some rooms'
                    WHEN 'منتجع صحي' THEN 'Spa'
                    WHEN 'إفطار مدفوع' THEN 'Paid breakfast'
                    WHEN 'حوض استحمام ساخن' THEN 'Hot tub'
                    WHEN 'موقف سيارات برسوم مدفوعة' THEN 'Paid parking'
                    WHEN 'مركز أعمال' THEN 'Business center'
                    WHEN 'الفطور' THEN 'Breakfast'
                    WHEN 'يُحظر التدخين' THEN 'No smoking'
                    WHEN 'مسبح' THEN 'Pool'
                    WHEN 'مطبخ في جميع الغرف' THEN 'Kitchen in all rooms'
                    WHEN 'موقف سيارات' THEN 'Parking'
                    WHEN 'يُسمح بحيوانات أليفة' THEN 'Pet-friendly'
                    WHEN 'حمام سباحة داخلي وخارجي' THEN 'Indoor and outdoor pool'
                    WHEN 'ملعب غولف' THEN 'Golf course'
                    WHEN 'اتصال Wi-Fi برسوم مدفوعة' THEN 'Paid Wi-Fi'
                    ELSE "NameEn"
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
