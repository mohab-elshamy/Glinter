using Glinter.Modules.Regions.Application.DTOs;

namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IRegionReferenceService
{
    Task<RegionReferenceDto?> GetNeighbourhoodAsync(
        int adm3Gid,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, RegionReferenceDto>> GetNeighbourhoodsAsync(
        IEnumerable<int> adm3Gids,
        CancellationToken cancellationToken = default);
}
