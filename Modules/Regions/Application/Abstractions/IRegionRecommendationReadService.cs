namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IRegionRecommendationReadService
{
    Task<IReadOnlyDictionary<int, RegionRecommendationNames>> ResolveAsync(
        IReadOnlyCollection<RegionRecommendationReference> references,
        CancellationToken cancellationToken = default);

    Task<RegionNameResolution> ResolveNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<RegionCentroidResolution?> ResolveCentroidAsync(
        int? adm0Gid,
        int? adm1Gid,
        int? adm2Gid,
        int? adm3Gid,
        CancellationToken cancellationToken = default);
}

public sealed record RegionRecommendationReference(
    int Key,
    int? Adm0Gid,
    int? Adm1Gid,
    int? Adm2Gid,
    int? Adm3Gid);

public sealed record RegionRecommendationNames(
    string? CountryNameEn,
    string? CountryNameAr,
    string? GovernorateNameEn,
    string? GovernorateNameAr,
    string? DistrictNameEn,
    string? DistrictNameAr,
    string? NeighbourhoodNameEn,
    string? NeighbourhoodNameAr,
    string? DisplayName);

public sealed record RegionNameResolution(
    bool IsResolved,
    bool IsAmbiguous,
    int? Adm0Gid,
    int? Adm1Gid,
    int? Adm2Gid,
    int? Adm3Gid,
    string? DisplayName);

public sealed record RegionCentroidResolution(
    int RegionId,
    string AdministrativeLevel,
    string? Name,
    double Latitude,
    double Longitude);
