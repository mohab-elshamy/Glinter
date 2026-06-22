using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Services;

public sealed class RegionReferenceService(RegionsDbContext dbContext) : IRegionReferenceService
{
    public async Task<RegionReferenceDto?> GetNeighbourhoodAsync(
        int adm3Gid,
        CancellationToken cancellationToken = default)
    {
        if (adm3Gid <= 0)
        {
            return null;
        }

        return await BuildQuery()
            .FirstOrDefaultAsync(x => x.Adm3Gid == adm3Gid, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, RegionReferenceDto>> GetNeighbourhoodsAsync(
        IEnumerable<int> adm3Gids,
        CancellationToken cancellationToken = default)
    {
        var ids = adm3Gids
            .Where(x => x > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<int, RegionReferenceDto>();
        }

        return await BuildQuery()
            .Where(x => ids.Contains(x.Adm3Gid))
            .ToDictionaryAsync(x => x.Adm3Gid, cancellationToken);
    }

    private IQueryable<RegionReferenceDto> BuildQuery()
    {
        return dbContext.Adm3
            .AsNoTracking()
            .Select(x => new RegionReferenceDto
            {
                Adm3Gid = x.Gid,
                NeighbourhoodNameEn = x.NameEn,
                NeighbourhoodNameAr = x.NameAr,
                Adm2Gid = x.Adm2Gid,
                DistrictNameEn = x.District.NameEn,
                DistrictNameAr = x.District.NameAr,
                Adm1Gid = x.District.Adm1Gid,
                GovernorateNameEn = x.District.Governorate.NameEn,
                GovernorateNameAr = x.District.Governorate.NameAr,
                Adm0Gid = x.District.Governorate.Adm0Gid,
                CountryNameEn = x.District.Governorate.Country.NameEn,
                CountryNameAr = x.District.Governorate.Country.NameAr
            });
    }
}
