using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.DTOs;

namespace Glinter.Modules.Regions.Application.PointLookup.Queries;

public class GetRegionsByPointQuery
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}

public class GetRegionsByPointHandler(IRegionsPointLookupRepository repository)
{
    public Task<RegionHierarchyGidsDto?> HandleAsync(GetRegionsByPointQuery query, CancellationToken ct)
        => repository.GetHierarchyByPointAsync(query.Lat, query.Lon, ct);
}
