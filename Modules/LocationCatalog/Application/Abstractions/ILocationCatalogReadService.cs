namespace Glinter.Modules.LocationCatalog.Application.Abstractions;

public interface ILocationCatalogReadService
{
    Task<bool> AreaExistsAsync(Guid areaId, CancellationToken cancellationToken = default);

    Task<AreaLocationDto?> GetAreaAsync(Guid areaId, CancellationToken cancellationToken = default);
}

public sealed record AreaLocationDto(
    Guid AreaId,
    string AreaNameEn,
    string? AreaNameAr,
    Guid DistrictId,
    string DistrictNameEn,
    Guid GovernorateId,
    string GovernorateNameEn,
    Guid CountryId,
    string CountryNameEn
);