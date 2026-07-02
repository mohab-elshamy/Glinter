using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Services;

public sealed class RegionRecommendationReadService(
    RegionsDbContext dbContext) : IRegionRecommendationReadService
{
    public async Task<IReadOnlyDictionary<int, RegionRecommendationNames>> ResolveAsync(
        IReadOnlyCollection<RegionRecommendationReference> references,
        CancellationToken cancellationToken = default)
    {
        if (references.Count == 0)
            return new Dictionary<int, RegionRecommendationNames>();

        var adm0Ids = references
            .Where(x => x.Adm0Gid is not null)
            .Select(x => x.Adm0Gid!.Value)
            .Distinct()
            .ToArray();
        var adm1Ids = references
            .Where(x => x.Adm1Gid is not null)
            .Select(x => x.Adm1Gid!.Value)
            .Distinct()
            .ToArray();
        var adm2Ids = references
            .Where(x => x.Adm2Gid is not null)
            .Select(x => x.Adm2Gid!.Value)
            .Distinct()
            .ToArray();
        var adm3Ids = references
            .Where(x => x.Adm3Gid is not null)
            .Select(x => x.Adm3Gid!.Value)
            .Distinct()
            .ToArray();

        var adm0 = await dbContext.Adm0
            .AsNoTracking()
            .Where(x => adm0Ids.Contains(x.Gid))
            .Select(x => new RegionName(x.Gid, x.NameEn, x.NameAr))
            .ToDictionaryAsync(x => x.Gid, cancellationToken);
        var adm1 = await dbContext.Adm1
            .AsNoTracking()
            .Where(x => adm1Ids.Contains(x.Gid))
            .Select(x => new RegionName(x.Gid, x.NameEn, x.NameAr))
            .ToDictionaryAsync(x => x.Gid, cancellationToken);
        var adm2 = await dbContext.Adm2
            .AsNoTracking()
            .Where(x => adm2Ids.Contains(x.Gid))
            .Select(x => new RegionName(x.Gid, x.NameEn, x.NameAr))
            .ToDictionaryAsync(x => x.Gid, cancellationToken);
        var adm3 = await dbContext.Adm3
            .AsNoTracking()
            .Where(x => adm3Ids.Contains(x.Gid))
            .Select(x => new RegionName(x.Gid, x.NameEn, x.NameAr))
            .ToDictionaryAsync(x => x.Gid, cancellationToken);

        return references.ToDictionary(
            x => x.Key,
            x =>
            {
                var country = Resolve(x.Adm0Gid, adm0);
                var governorate = Resolve(x.Adm1Gid, adm1);
                var district = Resolve(x.Adm2Gid, adm2);
                var neighbourhood = Resolve(x.Adm3Gid, adm3);

                return new RegionRecommendationNames(
                    country?.NameEn,
                    country?.NameAr,
                    governorate?.NameEn,
                    governorate?.NameAr,
                    district?.NameEn,
                    district?.NameAr,
                    neighbourhood?.NameEn,
                    neighbourhood?.NameAr,
                    BuildDisplayName(
                        neighbourhood,
                        district,
                        governorate,
                        country));
            });
    }

    private static RegionName? Resolve(
        int? id,
        IReadOnlyDictionary<int, RegionName> names) =>
        id is not null && names.TryGetValue(id.Value, out var name)
            ? name
            : null;

    private static string? BuildDisplayName(params RegionName?[] regions)
    {
        var names = regions
            .Where(x => x is not null)
            .Select(x => !string.IsNullOrWhiteSpace(x!.NameEn)
                ? x.NameEn
                : x.NameAr)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return names.Length == 0 ? null : string.Join(", ", names);
    }

    private sealed record RegionName(int Gid, string? NameEn, string? NameAr);
}
