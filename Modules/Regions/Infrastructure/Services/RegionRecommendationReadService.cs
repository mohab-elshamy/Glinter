using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Services;

public sealed class RegionRecommendationReadService(
    RegionsDbContext dbContext) : IRegionRecommendationReadService
{
    public async Task<RegionCentroidResolution?> ResolveCentroidAsync(
        int? adm0Gid,
        int? adm1Gid,
        int? adm2Gid,
        int? adm3Gid,
        CancellationToken cancellationToken = default)
    {
        if (adm3Gid is not null)
        {
            var value = await dbContext.Adm3.AsNoTracking()
                .Where(x => x.Gid == adm3Gid)
                .Select(x => new { x.Gid, Name = x.NameEn ?? x.NameAr, Geometry = x.BoundaryGeom })
                .SingleOrDefaultAsync(cancellationToken);
            return value is null ? null : ToCentroid(value.Gid, "Adm3", value.Name, value.Geometry);
        }
        if (adm2Gid is not null)
        {
            var value = await dbContext.Adm2.AsNoTracking()
                .Where(x => x.Gid == adm2Gid)
                .Select(x => new { x.Gid, Name = x.NameEn ?? x.NameAr, Geometry = x.BoundaryGeom })
                .SingleOrDefaultAsync(cancellationToken);
            return value is null ? null : ToCentroid(value.Gid, "Adm2", value.Name, value.Geometry);
        }
        if (adm1Gid is not null)
        {
            var value = await dbContext.Adm1.AsNoTracking()
                .Where(x => x.Gid == adm1Gid)
                .Select(x => new { x.Gid, Name = x.NameEn ?? x.NameAr, Geometry = x.BoundaryGeom })
                .SingleOrDefaultAsync(cancellationToken);
            return value is null ? null : ToCentroid(value.Gid, "Adm1", value.Name, value.Geometry);
        }
        if (adm0Gid is not null)
        {
            var value = await dbContext.Adm0.AsNoTracking()
                .Where(x => x.Gid == adm0Gid)
                .Select(x => new { x.Gid, Name = x.NameEn ?? x.NameAr, Geometry = x.BoundaryGeom })
                .SingleOrDefaultAsync(cancellationToken);
            return value is null ? null : ToCentroid(value.Gid, "Adm0", value.Name, value.Geometry);
        }
        return null;
    }
    public async Task<RegionNameResolution> ResolveNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeName(name);
        if (normalized.Length is < 2 or > 120)
            return new RegionNameResolution(false, false, null, null, null, null, null);
        var aliases = GetAliases(normalized);

        var adm3 = await dbContext.Adm3
            .AsNoTracking()
            .Where(x =>
                (x.NameEn != null && aliases.Contains(x.NameEn.ToLower())) ||
                (x.NameAr != null && aliases.Contains(x.NameAr.ToLower())))
            .Select(x => new RegionNameMatch(
                x.District.Governorate.Country.Gid,
                x.District.Governorate.Gid,
                x.District.Gid,
                x.Gid,
                x.NameEn ?? x.NameAr))
            .Take(2)
            .ToListAsync(cancellationToken);
        var adm2 = await dbContext.Adm2
            .AsNoTracking()
            .Where(x =>
                aliases.Contains(x.NameEn.ToLower()) ||
                (x.NameAr != null && aliases.Contains(x.NameAr.ToLower())))
            .Select(x => new RegionNameMatch(
                x.Governorate.Country.Gid,
                x.Governorate.Gid,
                x.Gid,
                null,
                x.NameEn ?? x.NameAr))
            .Take(2)
            .ToListAsync(cancellationToken);
        var adm1 = await dbContext.Adm1
            .AsNoTracking()
            .Where(x =>
                aliases.Contains(x.NameEn.ToLower()) ||
                (x.NameAr != null && aliases.Contains(x.NameAr.ToLower())))
            .Select(x => new RegionNameMatch(
                x.Country.Gid,
                x.Gid,
                null,
                null,
                x.NameEn ?? x.NameAr))
            .Take(2)
            .ToListAsync(cancellationToken);
        return ToResolution(adm3.Concat(adm2).Concat(adm1).Take(2).ToList());
    }

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

    private static RegionCentroidResolution? ToCentroid(
        int id,
        string level,
        string? name,
        NetTopologySuite.Geometries.Geometry? geometry)
    {
        if (geometry is null || geometry.IsEmpty)
            return null;
        var point = geometry.Centroid;
        return new RegionCentroidResolution(id, level, name, point.Y, point.X);
    }

    private static string NormalizeName(string value) =>
        string.Join(' ', value.Trim().ToLowerInvariant()
            .Replace('أ', 'ا')
            .Replace('إ', 'ا')
            .Replace('آ', 'ا')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string[] GetAliases(string normalized)
    {
        var aliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["downtown cairo"] = ["downtown cairo", "downtown", "وسط البلد"],
            ["وسط البلد"] = ["downtown cairo", "downtown", "وسط البلد"],
            ["zamalek"] = ["zamalek", "الزمالك"],
            ["الزمالك"] = ["zamalek", "الزمالك"],
            ["cairo"] = ["cairo", "القاهرة", "القاهره"],
            ["القاهرة"] = ["cairo", "القاهرة", "القاهره"],
            ["giza"] = ["giza", "الجيزة", "الجيزه"],
            ["الجيزة"] = ["giza", "الجيزة", "الجيزه"],
            ["alexandria"] = ["alexandria", "الإسكندرية", "الاسكندرية"],
            ["الاسكندرية"] = ["alexandria", "الإسكندرية", "الاسكندرية"],
            ["luxor"] = ["luxor", "الأقصر", "الاقصر"],
            ["الاقصر"] = ["luxor", "الأقصر", "الاقصر"],
            ["aswan"] = ["aswan", "أسوان", "اسوان"],
            ["اسوان"] = ["aswan", "أسوان", "اسوان"]
        };
        return aliases.TryGetValue(normalized, out var values)
            ? values.Select(x => x.ToLowerInvariant()).Distinct().ToArray()
            : [normalized];
    }

    private static RegionNameResolution ToResolution(
        IReadOnlyList<RegionNameMatch> matches)
    {
        if (matches.Count == 0)
            return new RegionNameResolution(false, false, null, null, null, null, null);
        if (matches.Count > 1)
            return new RegionNameResolution(false, true, null, null, null, null, null);
        var match = matches[0];
        return new RegionNameResolution(
            true,
            false,
            match.Adm0Gid,
            match.Adm1Gid,
            match.Adm2Gid,
            match.Adm3Gid,
            match.DisplayName);
    }

    private sealed record RegionNameMatch(
        int? Adm0Gid,
        int? Adm1Gid,
        int? Adm2Gid,
        int? Adm3Gid,
        string? DisplayName);
}
