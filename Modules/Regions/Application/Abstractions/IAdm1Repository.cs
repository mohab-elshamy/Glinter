using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;

namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IAdm1Repository
{
    Task<List<Adm1>> GetAllAsync(RegionListQuery query, CancellationToken ct = default);
    Task<List<Adm1>> GetByAdm0Async(int adm0Gid, RegionListQuery query, CancellationToken ct = default);
    Task<Adm1?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default);
    Task<RegionGeometryGeoJson?> GetGeometryGeoJsonAsync(int gid, int geometryAccuracy, CancellationToken ct = default);
    Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(IReadOnlyCollection<int> gids, int geometryAccuracy, CancellationToken ct = default);
    Task<Adm1?> GetByPcodeAsync(string pcode, CancellationToken ct = default);
    Task<Adm1> AddAsync(Adm1 entity, CancellationToken ct = default);
    Task<Adm1?> UpdateAsync(Adm1 entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int gid, CancellationToken ct = default);
}
