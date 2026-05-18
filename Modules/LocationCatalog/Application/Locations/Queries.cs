using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Locations;

public sealed class GetCountriesQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public GetCountriesQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CountryResponse>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Countries
            .AsNoTracking()
            .OrderBy(x => x.NameEn)
            .Select(x => new CountryResponse(
                x.Id,
                x.Pcode,
                x.NameEn,
                x.NameAr
            ))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetGovernoratesByCountryQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public GetGovernoratesByCountryQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<GovernorateResponse>> HandleAsync(
        Guid countryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Governorates
            .AsNoTracking()
            .Where(x => x.CountryId == countryId)
            .OrderBy(x => x.NameEn)
            .Select(x => new GovernorateResponse(
                x.Id,
                x.CountryId,
                x.Pcode,
                x.NameEn,
                x.NameAr
            ))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetDistrictsByGovernorateQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public GetDistrictsByGovernorateQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DistrictResponse>> HandleAsync(
        Guid governorateId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Districts
            .AsNoTracking()
            .Where(x => x.GovernorateId == governorateId)
            .OrderBy(x => x.NameEn)
            .Select(x => new DistrictResponse(
                x.Id,
                x.GovernorateId,
                x.Pcode,
                x.NameEn,
                x.NameAr
            ))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetAreasByDistrictQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public GetAreasByDistrictQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AreaResponse>> HandleAsync(
        Guid districtId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Areas
            .AsNoTracking()
            .Where(x => x.DistrictId == districtId && x.IsActive)
            .OrderBy(x => x.NameEn)
            .Select(x => new AreaResponse(
                x.Id,
                x.DistrictId,
                x.Pcode,
                x.NameEn,
                x.NameAr,
                x.Latitude,
                x.Longitude,
                x.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}

public sealed class SearchAreasQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public SearchAreasQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AreaSearchResponse>> HandleAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return new List<AreaSearchResponse>();
        }

        var normalizedQuery = query.Trim().ToLower();
        var originalQuery = query.Trim();

        var results = await _dbContext.Areas
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x =>
                x.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.NameAr != null && x.NameAr.Contains(originalQuery)) ||
                x.Pcode.ToLower().Contains(normalizedQuery) ||

                x.District.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.District.NameAr != null && x.District.NameAr.Contains(originalQuery)) ||
                x.District.Pcode.ToLower().Contains(normalizedQuery) ||

                x.District.Governorate.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.District.Governorate.NameAr != null && x.District.Governorate.NameAr.Contains(originalQuery)) ||
                x.District.Governorate.Pcode.ToLower().Contains(normalizedQuery) ||

                x.District.Governorate.Country.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.District.Governorate.Country.NameAr != null && x.District.Governorate.Country.NameAr.Contains(originalQuery)) ||
                x.District.Governorate.Country.Pcode.ToLower().Contains(normalizedQuery))
            .OrderBy(x => x.NameEn.ToLower().StartsWith(normalizedQuery) ? 0 : 1)
            .ThenBy(x => x.District.Governorate.NameEn)
            .ThenBy(x => x.District.NameEn)
            .ThenBy(x => x.NameEn)
            .Take(50)
            .Select(x => new
            {
                AreaId = x.Id,
                AreaNameEn = x.NameEn,
                AreaNameAr = x.NameAr,
                DistrictId = x.DistrictId,
                DistrictNameEn = x.District.NameEn,
                DistrictNameAr = x.District.NameAr,
                GovernorateId = x.District.Governorate.Id,
                GovernorateNameEn = x.District.Governorate.NameEn,
                GovernorateNameAr = x.District.Governorate.NameAr,
                CountryId = x.District.Governorate.Country.Id,
                CountryNameEn = x.District.Governorate.Country.NameEn,
                CountryNameAr = x.District.Governorate.Country.NameAr,
                x.Latitude,
                x.Longitude
            })
            .ToListAsync(cancellationToken);

        return results
            .Select(x => new AreaSearchResponse(
                x.AreaId,
                x.AreaNameEn,
                x.AreaNameAr,
                BuildDisplayName(
                    x.AreaNameEn,
                    x.AreaNameAr,
                    x.DistrictNameEn,
                    x.GovernorateNameEn,
                    x.CountryNameEn),
                x.DistrictId,
                x.DistrictNameEn,
                x.DistrictNameAr,
                x.GovernorateId,
                x.GovernorateNameEn,
                x.GovernorateNameAr,
                x.CountryId,
                x.CountryNameEn,
                x.CountryNameAr,
                x.Latitude,
                x.Longitude
            ))
            .ToList();
    }

    private static string BuildDisplayName(
        string areaNameEn,
        string? areaNameAr,
        string districtNameEn,
        string governorateNameEn,
        string countryNameEn)
    {
        var areaDisplayName = !string.IsNullOrWhiteSpace(areaNameEn)
            ? areaNameEn
            : areaNameAr ?? string.Empty;

        return $"{areaDisplayName}, {districtNameEn}, {governorateNameEn}, {countryNameEn}";
    }
}
public sealed class GetAreaByIdQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public GetAreaByIdQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AreaDetailsResponse?> HandleAsync(
        Guid areaId,
        CancellationToken cancellationToken = default)
    {
        var area = await _dbContext.Areas
            .AsNoTracking()
            .Include(x => x.District)
            .ThenInclude(x => x.Governorate)
            .ThenInclude(x => x.Country)
            .FirstOrDefaultAsync(x => x.Id == areaId && x.IsActive, cancellationToken);

        if (area is null)
        {
            return null;
        }

        return new AreaDetailsResponse(
            area.Id,
            area.NameEn,
            area.NameAr,
            area.Latitude,
            area.Longitude,
            area.District.Id,
            area.District.NameEn,
            area.District.Governorate.Id,
            area.District.Governorate.NameEn,
            area.District.Governorate.Country.Id,
            area.District.Governorate.Country.NameEn
        );
    }
    
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
}