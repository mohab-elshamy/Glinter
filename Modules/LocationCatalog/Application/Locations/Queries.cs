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

    public async Task<List<AreaResponse>> HandleAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var areasQuery = _dbContext.Areas
            .AsNoTracking()
            .Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim().ToLower();

            areasQuery = areasQuery.Where(x =>
                x.NameEn.ToLower().Contains(normalizedQuery) ||
                (x.NameAr != null && x.NameAr.Contains(query.Trim())) ||
                x.Pcode.ToLower().Contains(normalizedQuery));
        }

        return await areasQuery
            .OrderBy(x => x.NameEn)
            .Take(50)
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
}