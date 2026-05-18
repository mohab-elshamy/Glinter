using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Glinter.Modules.LocationCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Persistence.Import;

public static class HdxLocationCatalogImporter
{
    public static async Task ImportAsync(
        LocationCatalogDbContext dbContext,
        string csvPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"HDX CSV file was not found at path: {csvPath}");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        await using var fileStream = new FileStream(
            csvPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, config);

        var rows = csv.GetRecords<HdxLocationCsvRow>()
            .Where(IsValidRow)
            .ToList();

        var countries = await dbContext.Countries
            .ToDictionaryAsync(x => x.Pcode, cancellationToken);

        var governorates = await dbContext.Governorates
            .ToDictionaryAsync(x => x.Pcode, cancellationToken);

        var districts = await dbContext.Districts
            .ToDictionaryAsync(x => x.Pcode, cancellationToken);

        var areas = await dbContext.Areas
            .ToDictionaryAsync(x => x.Pcode, cancellationToken);

        foreach (var row in rows)
        {
            var country = GetOrCreateCountry(row, countries);
            var governorate = GetOrCreateGovernorate(row, country.Id, governorates);
            var district = GetOrCreateDistrict(row, governorate.Id, districts);
            var area = GetOrCreateArea(row, district.Id, areas);

            area.DistrictId = district.Id;

            area.NameEn = GetEnglishName(
                row.Adm3Pcode,
                row.Adm3RefName,
                row.Adm3Name,
                row.Adm3Name1,
                row.Adm3Name2,
                row.Adm3Name3);

            area.NameAr = GetArabicName(
                row.Adm3Name,
                row.Adm3Name1,
                row.Adm3Name2,
                row.Adm3Name3);

            area.Latitude = row.Latitude;
            area.Longitude = row.Longitude;
            area.IsActive = true;
        }

        await AddNewEntitiesAsync(
            dbContext,
            countries.Values,
            governorates.Values,
            districts.Values,
            areas.Values,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsValidRow(HdxLocationCsvRow row)
    {
        return !string.IsNullOrWhiteSpace(row.Adm0Pcode)
               && !string.IsNullOrWhiteSpace(row.Adm1Pcode)
               && !string.IsNullOrWhiteSpace(row.Adm2Pcode)
               && !string.IsNullOrWhiteSpace(row.Adm3Pcode);
    }

    private static Country GetOrCreateCountry(
        HdxLocationCsvRow row,
        Dictionary<string, Country> countries)
    {
        var pcode = row.Adm0Pcode.Trim();

        if (countries.TryGetValue(pcode, out var existing))
        {
            existing.NameEn = GetEnglishName(
                row.Adm0Pcode,
                row.Adm0Name,
                row.Adm0Name1,
                row.Adm0Name2,
                row.Adm0Name3);

            existing.NameAr = GetArabicName(
                row.Adm0Name,
                row.Adm0Name1,
                row.Adm0Name2,
                row.Adm0Name3);

            return existing;
        }

        var country = new Country
        {
            Id = Guid.NewGuid(),
            Pcode = pcode,
            NameEn = GetEnglishName(
                row.Adm0Pcode,
                row.Adm0Name,
                row.Adm0Name1,
                row.Adm0Name2,
                row.Adm0Name3),
            NameAr = GetArabicName(
                row.Adm0Name,
                row.Adm0Name1,
                row.Adm0Name2,
                row.Adm0Name3),
            CreatedAtUtc = DateTime.UtcNow
        };

        countries[pcode] = country;

        return country;
    }

    private static Governorate GetOrCreateGovernorate(
        HdxLocationCsvRow row,
        Guid countryId,
        Dictionary<string, Governorate> governorates)
    {
        var pcode = row.Adm1Pcode.Trim();

        if (governorates.TryGetValue(pcode, out var existing))
        {
            existing.CountryId = countryId;

            existing.NameEn = GetEnglishName(
                row.Adm1Pcode,
                row.Adm1Name,
                row.Adm1Name1,
                row.Adm1Name2,
                row.Adm1Name3);

            existing.NameAr = GetArabicName(
                row.Adm1Name,
                row.Adm1Name1,
                row.Adm1Name2,
                row.Adm1Name3);

            return existing;
        }

        var governorate = new Governorate
        {
            Id = Guid.NewGuid(),
            CountryId = countryId,
            Pcode = pcode,
            NameEn = GetEnglishName(
                row.Adm1Pcode,
                row.Adm1Name,
                row.Adm1Name1,
                row.Adm1Name2,
                row.Adm1Name3),
            NameAr = GetArabicName(
                row.Adm1Name,
                row.Adm1Name1,
                row.Adm1Name2,
                row.Adm1Name3),
            CreatedAtUtc = DateTime.UtcNow
        };

        governorates[pcode] = governorate;

        return governorate;
    }

    private static District GetOrCreateDistrict(
        HdxLocationCsvRow row,
        Guid governorateId,
        Dictionary<string, District> districts)
    {
        var pcode = row.Adm2Pcode.Trim();

        if (districts.TryGetValue(pcode, out var existing))
        {
            existing.GovernorateId = governorateId;

            existing.NameEn = GetEnglishName(
                row.Adm2Pcode,
                row.Adm2Name,
                row.Adm2Name1,
                row.Adm2Name2,
                row.Adm2Name3);

            existing.NameAr = GetArabicName(
                row.Adm2Name,
                row.Adm2Name1,
                row.Adm2Name2,
                row.Adm2Name3);

            return existing;
        }

        var district = new District
        {
            Id = Guid.NewGuid(),
            GovernorateId = governorateId,
            Pcode = pcode,
            NameEn = GetEnglishName(
                row.Adm2Pcode,
                row.Adm2Name,
                row.Adm2Name1,
                row.Adm2Name2,
                row.Adm2Name3),
            NameAr = GetArabicName(
                row.Adm2Name,
                row.Adm2Name1,
                row.Adm2Name2,
                row.Adm2Name3),
            CreatedAtUtc = DateTime.UtcNow
        };

        districts[pcode] = district;

        return district;
    }

    private static Area GetOrCreateArea(
        HdxLocationCsvRow row,
        Guid districtId,
        Dictionary<string, Area> areas)
    {
        var pcode = row.Adm3Pcode.Trim();

        if (areas.TryGetValue(pcode, out var existing))
        {
            return existing;
        }

        var area = new Area
        {
            Id = Guid.NewGuid(),
            DistrictId = districtId,
            Pcode = pcode,
            NameEn = GetEnglishName(
                row.Adm3Pcode,
                row.Adm3RefName,
                row.Adm3Name,
                row.Adm3Name1,
                row.Adm3Name2,
                row.Adm3Name3),
            NameAr = GetArabicName(
                row.Adm3Name,
                row.Adm3Name1,
                row.Adm3Name2,
                row.Adm3Name3),
            Latitude = row.Latitude,
            Longitude = row.Longitude,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        areas[pcode] = area;

        return area;
    }

    private static async Task AddNewEntitiesAsync(
        LocationCatalogDbContext dbContext,
        IEnumerable<Country> countries,
        IEnumerable<Governorate> governorates,
        IEnumerable<District> districts,
        IEnumerable<Area> areas,
        CancellationToken cancellationToken)
    {
        var existingCountryIds = await dbContext.Countries
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingGovernorateIds = await dbContext.Governorates
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingDistrictIds = await dbContext.Districts
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var existingAreaIds = await dbContext.Areas
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await dbContext.Countries.AddRangeAsync(
            countries.Where(x => !existingCountryIds.Contains(x.Id)),
            cancellationToken);

        await dbContext.Governorates.AddRangeAsync(
            governorates.Where(x => !existingGovernorateIds.Contains(x.Id)),
            cancellationToken);

        await dbContext.Districts.AddRangeAsync(
            districts.Where(x => !existingDistrictIds.Contains(x.Id)),
            cancellationToken);

        await dbContext.Areas.AddRangeAsync(
            areas.Where(x => !existingAreaIds.Contains(x.Id)),
            cancellationToken);
    }

    private static string GetEnglishName(string fallbackPcode, params string?[] values)
    {
        var englishName = values
            .Where(IsUsefulName)
            .Select(x => x!.Trim())
            .FirstOrDefault(x => !ContainsArabic(x));

        if (!string.IsNullOrWhiteSpace(englishName))
        {
            return englishName;
        }

        var arabicName = values
            .Where(IsUsefulName)
            .Select(x => x!.Trim())
            .FirstOrDefault(ContainsArabic);

        if (!string.IsNullOrWhiteSpace(arabicName))
        {
            return arabicName;
        }

        return fallbackPcode;
    }

    private static string? GetArabicName(params string?[] values)
    {
        return values
            .Where(IsUsefulName)
            .Select(x => x!.Trim())
            .FirstOrDefault(ContainsArabic);
    }

    private static bool IsUsefulName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        return !normalized.Equals("None", StringComparison.OrdinalIgnoreCase)
               && !normalized.Equals("Null", StringComparison.OrdinalIgnoreCase)
               && !normalized.Equals("N/A", StringComparison.OrdinalIgnoreCase)
               && !normalized.Equals("NA", StringComparison.OrdinalIgnoreCase)
               && normalized != "-"
               && normalized != "--";
    }

    private static bool ContainsArabic(string value)
    {
        return value.Any(c => c >= '\u0600' && c <= '\u06FF');
    }
}