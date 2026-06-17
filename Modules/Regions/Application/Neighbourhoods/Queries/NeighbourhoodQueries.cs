using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using System.Text.Json;

namespace Glinter.Modules.Regions.Application.Neighbourhoods.Queries;

public class GetNeighbourhoodByIdQuery
{
    public int Gid { get; set; }
    public int GeometryAccuracy { get; set; } = 0;
}
public class GetAllNeighbourhoodsQuery : RegionListQuery { }
public class GetNeighbourhoodsByDistrictQuery : RegionListQuery { public int Adm2Gid { get; set; } }

public class GetNeighbourhoodByIdHandler(IAdm3Repository repository)
{
    public async Task<Adm3Dto?> HandleAsync(GetNeighbourhoodByIdQuery query, CancellationToken ct)
    {
        var e = await repository.GetByIdAsync(query.Gid, false, ct);
        if (e is null) return null;

        JsonElement? geometry = null;
        if (query.GeometryAccuracy > 0)
        {
            var result = await repository.GetGeometryGeoJsonAsync(query.Gid, query.GeometryAccuracy, ct);
            geometry = GeoJsonSerializationHelper.ToJsonElement(result?.GeoJson);
        }

        return NeighbourhoodQueryMapper.Map(e, geometry);
    }
}

public class GetAllNeighbourhoodsHandler(IAdm3Repository repository)
{
    public async Task<List<Adm3Dto>> HandleAsync(GetAllNeighbourhoodsQuery query, CancellationToken ct)
    {
        var list = await repository.GetAllAsync(query, ct);
        var geometries = await NeighbourhoodQueryMapper.GetGeometryMapAsync(repository, list, query, ct);
        return list.ConvertAll(e => NeighbourhoodQueryMapper.Map(e, geometries.GetValueOrDefault(e.Gid)));
    }
}

public class GetNeighbourhoodsByDistrictHandler(IAdm3Repository repository)
{
    public async Task<List<Adm3Dto>> HandleAsync(GetNeighbourhoodsByDistrictQuery query, CancellationToken ct)
    {
        var list = await repository.GetByAdm2Async(query.Adm2Gid, query, ct);
        var geometries = await NeighbourhoodQueryMapper.GetGeometryMapAsync(repository, list, query, ct);
        return list.ConvertAll(e => NeighbourhoodQueryMapper.Map(e, geometries.GetValueOrDefault(e.Gid)));
    }
}

internal static class NeighbourhoodQueryMapper
{
    internal static async Task<Dictionary<int, JsonElement?>> GetGeometryMapAsync(
        IAdm3Repository repository,
        List<Adm3> list,
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

    internal static Adm3Dto Map(Adm3 e, JsonElement? geometryGeoJson)
    {
        return new Adm3Dto
        {
            Gid = e.Gid,
            Adm2Gid = e.Adm2Gid,
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
