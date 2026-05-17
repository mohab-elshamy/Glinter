using Glinter.Modules.LocationCatalog.Application.Abstractions;
using Glinter.Modules.LocationCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Services;

public sealed class LocationCatalogReadService : ILocationCatalogReadService
{
    private readonly LocationCatalogDbContext _dbContext;

    public LocationCatalogReadService(LocationCatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AreaExistsAsync(Guid areaId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Areas
            .AsNoTracking()
            .AnyAsync(x => x.Id == areaId && x.IsActive, cancellationToken);
    }

    public async Task<AreaLocationDto?> GetAreaAsync(Guid areaId, CancellationToken cancellationToken = default)
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

        return new AreaLocationDto(
            area.Id,
            area.NameEn,
            area.NameAr,
            area.District.Id,
            area.District.NameEn,
            area.District.Governorate.Id,
            area.District.Governorate.NameEn,
            area.District.Governorate.Country.Id,
            area.District.Governorate.Country.NameEn
        );
    }
}