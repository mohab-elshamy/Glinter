using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Locations;

public sealed class SearchDistrictsQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public SearchDistrictsQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DistrictSearchResponse>> HandleAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new List<DistrictSearchResponse>();
        }

        var normalizedQuery = query.Trim().ToLower();
        var originalQuery = query.Trim();
        var alternativeQuery = GetAlternativeSearchTerm(normalizedQuery);

        var results = await _dbContext.Districts
            .AsNoTracking()
            .Where(x =>
                x.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.NameAr != null && x.NameAr.Contains(originalQuery)) ||
                x.Pcode.ToLower().Contains(normalizedQuery) ||

                x.Governorate.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.Governorate.NameAr != null && x.Governorate.NameAr.Contains(originalQuery)) ||
                x.Governorate.Pcode.ToLower().Contains(normalizedQuery) ||

                x.Governorate.Country.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.Governorate.Country.NameAr != null && x.Governorate.Country.NameAr.Contains(originalQuery)) ||
                x.Governorate.Country.Pcode.ToLower().Contains(normalizedQuery) ||

                (
                    alternativeQuery != null &&
                    (
                        x.NameEn.ToLower().Contains(alternativeQuery) ||
                        x.Governorate.NameEn.ToLower().Contains(alternativeQuery)
                    )
                ))
            .OrderBy(x => x.NameEn.ToLower().StartsWith(normalizedQuery) ? 0 : 1)
            .ThenBy(x => x.Governorate.NameEn)
            .ThenBy(x => x.NameEn)
            .Take(50)
            .Select(x => new
            {
                DistrictId = x.Id,
                DistrictNameEn = x.NameEn,
                DistrictNameAr = x.NameAr,
                GovernorateId = x.Governorate.Id,
                GovernorateNameEn = x.Governorate.NameEn,
                GovernorateNameAr = x.Governorate.NameAr,
                CountryId = x.Governorate.Country.Id,
                CountryNameEn = x.Governorate.Country.NameEn,
                CountryNameAr = x.Governorate.Country.NameAr
            })
            .ToListAsync(cancellationToken);

        return results
            .Select(x =>
            {
                var friendlyDistrictName = GetFriendlyDistrictName(x.DistrictNameEn);

                return new DistrictSearchResponse(
                    x.DistrictId,
                    x.DistrictNameEn,
                    x.DistrictNameAr,
                    $"{friendlyDistrictName}, {x.GovernorateNameEn}, {x.CountryNameEn}",
                    x.GovernorateId,
                    x.GovernorateNameEn,
                    x.GovernorateNameAr,
                    x.CountryId,
                    x.CountryNameEn,
                    x.CountryNameAr
                );
            })
            .ToList();
    }

    private static string? GetAlternativeSearchTerm(string query)
    {
        return query switch
        {
            "zamalek" => "zamalik",
            "helwan" => "hilwan",
            "shorouk" => "shroq",
            "el shorouk" => "shroq",
            "15 may" => "15 mayu",
            "heliopolis" => "misr al-gadida",
            _ => null
        };
    }

    private static string GetFriendlyDistrictName(string districtNameEn)
    {
        return districtNameEn switch
        {
            "Zamalik" => "Zamalek",
            "Hilwan" => "Helwan",
            "Shroq" => "El Shorouk",
            "15 Mayu" => "15 May",
            "Misr al-Gadida" => "Heliopolis",
            _ => districtNameEn
        };
    }
}