using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;

namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IAdm3Repository
{
    Task<List<Adm3>> GetAllAsync(RegionListQuery query, CancellationToken ct = default);
    Task<List<Adm3>> GetByAdm2Async(int adm2Gid, RegionListQuery query, CancellationToken ct = default);
    Task<Adm3?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default);
    Task<RegionGeometryGeoJson?> GetGeometryGeoJsonAsync(int gid, int geometryAccuracy, CancellationToken ct = default);
    Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(IReadOnlyCollection<int> gids, int geometryAccuracy, CancellationToken ct = default);
    Task<Adm3?> GetByPcodeAsync(string pcode, CancellationToken ct = default);
    Task<Adm3> AddAsync(Adm3 entity, CancellationToken ct = default);
    Task<Adm3?> UpdateAsync(Adm3 entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int gid, CancellationToken ct = default);
}
