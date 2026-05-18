using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Application.Locations;

public sealed class GetDistrictIndexByDistrictQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public GetDistrictIndexByDistrictQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DistrictIndexResponse?> HandleAsync(
        Guid districtId,
        CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.DistrictIndices
            .AsNoTracking()
            .Where(x => x.DistrictId == districtId)
            .Select(x => new
            {
                x.DistrictId,
                DistrictNameEn = x.District.NameEn,
                DistrictNameAr = x.District.NameAr,
                GovernorateId = x.District.Governorate.Id,
                GovernorateNameEn = x.District.Governorate.NameEn,
                GovernorateNameAr = x.District.Governorate.NameAr,
                CountryId = x.District.Governorate.Country.Id,
                CountryNameEn = x.District.Governorate.Country.NameEn,
                CountryNameAr = x.District.Governorate.Country.NameAr,
                Latitude = _dbContext.Areas
                    .Where(a => a.DistrictId == x.DistrictId && a.Latitude != null)
                    .Average(a => (double?)a.Latitude),
                Longitude = _dbContext.Areas
                    .Where(a => a.DistrictId == x.DistrictId && a.Longitude != null)
                    .Average(a => (double?)a.Longitude),
                x.SafetyScore,
                x.SafetyLevel,
                x.SafetyExplanation,
                x.PriceScore,
                x.PriceLevel,
                x.PriceExplanation,
                x.ServicesScore,
                x.ServicesLevel,
                x.ServicesExplanation,
                x.ComputedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new DistrictIndexResponse(
            result.DistrictId,
            result.DistrictNameEn,
            result.DistrictNameAr,
            $"{result.DistrictNameEn}, {result.GovernorateNameEn}, {result.CountryNameEn}",
            result.GovernorateId,
            result.GovernorateNameEn,
            result.GovernorateNameAr,
            result.CountryId,
            result.CountryNameEn,
            result.CountryNameAr,
            result.Latitude,
            result.Longitude,
            result.SafetyScore,
            result.SafetyLevel,
            result.SafetyExplanation,
            result.PriceScore,
            result.PriceLevel,
            result.PriceExplanation,
            result.ServicesScore,
            result.ServicesLevel,
            result.ServicesExplanation,
            result.ComputedAtUtc
        );
    }
}

public sealed class GetGovernorateDistrictIndicesQueryHandler
{
    private readonly ILocationCatalogDbContext _dbContext;

    public GetGovernorateDistrictIndicesQueryHandler(ILocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DistrictIndexResponse>> HandleAsync(
        Guid governorateId,
        CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.DistrictIndices
            .AsNoTracking()
            .Where(x => x.District.GovernorateId == governorateId)
            .OrderBy(x => x.District.NameEn)
            .Select(x => new
            {
                x.DistrictId,
                DistrictNameEn = x.District.NameEn,
                DistrictNameAr = x.District.NameAr,
                GovernorateId = x.District.Governorate.Id,
                GovernorateNameEn = x.District.Governorate.NameEn,
                GovernorateNameAr = x.District.Governorate.NameAr,
                CountryId = x.District.Governorate.Country.Id,
                CountryNameEn = x.District.Governorate.Country.NameEn,
                CountryNameAr = x.District.Governorate.Country.NameAr,
                Latitude = _dbContext.Areas
                    .Where(a => a.DistrictId == x.DistrictId && a.Latitude != null)
                    .Average(a => (double?)a.Latitude),
                Longitude = _dbContext.Areas
                    .Where(a => a.DistrictId == x.DistrictId && a.Longitude != null)
                    .Average(a => (double?)a.Longitude),
                x.SafetyScore,
                x.SafetyLevel,
                x.SafetyExplanation,
                x.PriceScore,
                x.PriceLevel,
                x.PriceExplanation,
                x.ServicesScore,
                x.ServicesLevel,
                x.ServicesExplanation,
                x.ComputedAtUtc
            })
            .ToListAsync(cancellationToken);

        return results
            .Select(x => new DistrictIndexResponse(
                x.DistrictId,
                x.DistrictNameEn,
                x.DistrictNameAr,
                $"{x.DistrictNameEn}, {x.GovernorateNameEn}, {x.CountryNameEn}",
                x.GovernorateId,
                x.GovernorateNameEn,
                x.GovernorateNameAr,
                x.CountryId,
                x.CountryNameEn,
                x.CountryNameAr,
                x.Latitude,
                x.Longitude,
                x.SafetyScore,
                x.SafetyLevel,
                x.SafetyExplanation,
                x.PriceScore,
                x.PriceLevel,
                x.PriceExplanation,
                x.ServicesScore,
                x.ServicesLevel,
                x.ServicesExplanation,
                x.ComputedAtUtc
            ))
            .ToList();
    }
}