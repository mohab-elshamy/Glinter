using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;

namespace Glinter.Modules.Regions.Application.Abstractions;

public interface IAdm0Repository
{
    Task<List<Adm0>> GetAllAsync(RegionListQuery query, CancellationToken ct = default);
    Task<Adm0?> GetByIdAsync(int gid, bool loadGeometry = false, CancellationToken ct = default);
    Task<RegionGeometryGeoJson?> GetGeometryGeoJsonAsync(int gid, int geometryAccuracy, CancellationToken ct = default);
    Task<List<RegionGeometryGeoJson>> GetGeometryGeoJsonByIdsAsync(IReadOnlyCollection<int> gids, int geometryAccuracy, CancellationToken ct = default);
    Task<Adm0?> GetByPcodeAsync(string pcode, CancellationToken ct = default);
    Task<Adm0> AddAsync(Adm0 entity, CancellationToken ct = default);
    Task<Adm0?> UpdateAsync(Adm0 entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(int gid, CancellationToken ct = default);
}
