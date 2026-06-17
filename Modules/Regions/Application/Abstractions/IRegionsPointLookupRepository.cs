using Glinter.Modules.Regions.Application.DTOs;

namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IRegionsPointLookupRepository
{
    Task<RegionHierarchyGidsDto?> GetHierarchyByPointAsync(double lat, double lon, CancellationToken ct = default);
}
