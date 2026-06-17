using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;

namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IAdm2Repository
{
    Task<List<Adm2>> GetAllAsync(RegionListQuery query, CancellationToken ct = default);
    Task<List<Adm2>> GetByAdm1Async(int adm1Gid, RegionListQuery query, CancellationToken ct = default);
    Task<Adm2?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default);
    Task<RegionGeometryGeoJson?> GetGeometryGeoJsonAsync(int gid, int geometryAccuracy, CancellationToken ct = default);
    Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(IReadOnlyCollection<int> gids, int geometryAccuracy, CancellationToken ct = default);
    Task<Adm2?> GetByPcodeAsync(string pcode, CancellationToken ct = default);
    Task<Adm2> AddAsync(Adm2 entity, CancellationToken ct = default);
    Task<Adm2?> UpdateAsync(Adm2 entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int gid, CancellationToken ct = default);
}
