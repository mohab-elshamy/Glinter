using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using System.Text.Json;

namespace Glinter.Modules.Regions.Application.Districts.Queries;

public class GetDistrictByIdQuery
{
    public int Gid { get; set; }
    public int GeometryAccuracy { get; set; } = 0;
}
public class GetAllDistrictsQuery : RegionListQuery { }
public class GetDistrictsByGovernorateQuery : RegionListQuery { public int Adm1Gid { get; set; } }

public class GetDistrictByIdHandler(IAdm2Repository repository)
{
    public async Task<Adm2Dto?> HandleAsync(GetDistrictByIdQuery query, CancellationToken ct)
    {
        var e = await repository.GetByIdAsync(query.Gid, false, ct);
        if (e is null) return null;

        JsonElement? geometry = null;
        if (query.GeometryAccuracy > 0)
        {
            var result = await repository.GetGeometryGeoJsonAsync(query.Gid, query.GeometryAccuracy, ct);
            geometry = GeoJsonSerializationHelper.ToJsonElement(result?.GeoJson);
        }

        return DistrictQueryMapper.Map(e, geometry);
    }
}

public class GetAllDistrictsHandler(IAdm2Repository repository)
{
    public async Task<List<Adm2Dto>> HandleAsync(GetAllDistrictsQuery query, CancellationToken ct)
    {
        var list = await repository.GetAllAsync(query, ct);
        var geometries = await DistrictQueryMapper.GetGeometryMapAsync(repository, list, query, ct);
        return list.ConvertAll(e => DistrictQueryMapper.Map(e, geometries.GetValueOrDefault(e.Gid)));
    }
}

public class GetDistrictsByGovernorateHandler(IAdm2Repository repository)
{
    public async Task<List<Adm2Dto>> HandleAsync(GetDistrictsByGovernorateQuery query, CancellationToken ct)
    {
        var list = await repository.GetByAdm1Async(query.Adm1Gid, query, ct);
        var geometries = await DistrictQueryMapper.GetGeometryMapAsync(repository, list, query, ct);
        return list.ConvertAll(e => DistrictQueryMapper.Map(e, geometries.GetValueOrDefault(e.Gid)));
    }
}

internal static class DistrictQueryMapper
{
    internal static async Task<Dictionary<int, JsonElement?>> GetGeometryMapAsync(
        IAdm2Repository repository,
        List<Adm2> list,
        RegionListQuery query,
        CancellationToken ct)
    {
        if (query.GeometryAccuracy <= 0)
        {
            return [];
        }

        var geoJsonRows = await repository.GetGeometryGeoJsonByIdsAsync(
            list.Select(x => x.Gid).ToArray(),
            query.GeometryAccuracy,
            ct);

        return RegionGeometryMappingHelper.ToJsonByGid(geoJsonRows);
    }

    internal static Adm2Dto Map(Adm2 e, JsonElement? geometryGeoJson)
    {
        return new Adm2Dto
        {
            Gid = e.Gid,
            Adm1Gid = e.Adm1Gid,
            NameEn = e.NameEn,
            NameAr = e.NameAr,
            Pcode = e.Pcode,
            ImageUrl = e.ImageUrl,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            GeometryGeoJson = geometryGeoJson,
        };
    }
}
