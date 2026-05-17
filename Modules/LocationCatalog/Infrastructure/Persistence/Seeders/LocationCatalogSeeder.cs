using Glinter.Modules.LocationCatalog.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Persistence.Seeders;

public static class LocationCatalogSeeder
{
    public static async Task SeedAsync(LocationCatalogDbContext dbContext)
    {
        if (await dbContext.Countries.AnyAsync(x => x.Pcode == "EGY"))
        {
            return;
        }

        var country = new Country
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111100"),
            Pcode = "EGY",
            NameEn = "Egypt",
            NameAr = "مصر",
            CreatedAtUtc = DateTime.UtcNow
        };

        var governorate = new Governorate
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
            CountryId = country.Id,
            Pcode = "EGY-CAI",
            NameEn = "Cairo",
            NameAr = "القاهرة",
            CreatedAtUtc = DateTime.UtcNow
        };

        var district = new District
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
            GovernorateId = governorate.Id,
            Pcode = "EGY-CAI-001",
            NameEn = "Cairo District",
            NameAr = "قسم القاهرة",
            CreatedAtUtc = DateTime.UtcNow
        };

        var areas = new List<Area>
        {
            new Area
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                DistrictId = district.Id,
                Pcode = "EGY-CAI-001-ZAM",
                NameEn = "Zamalek",
                NameAr = "الزمالك",
                Latitude = 30.0618,
                Longitude = 31.2197,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Area
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                DistrictId = district.Id,
                Pcode = "EGY-CAI-001-MAA",
                NameEn = "Maadi",
                NameAr = "المعادي",
                Latitude = 29.9602,
                Longitude = 31.2569,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Area
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                DistrictId = district.Id,
                Pcode = "EGY-CAI-001-NAS",
                NameEn = "Nasr City",
                NameAr = "مدينة نصر",
                Latitude = 30.0561,
                Longitude = 31.3300,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        await dbContext.Countries.AddAsync(country);
        await dbContext.Governorates.AddAsync(governorate);
        await dbContext.Districts.AddAsync(district);
        await dbContext.Areas.AddRangeAsync(areas);

        await dbContext.SaveChangesAsync();
    }
}