using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using System.Text.Json;

namespace Glinter.Modules.Regions.Application.Governorates.Queries;

public class GetGovernorateByIdQuery
{
    public int Gid { get; set; }
    public int GeometryAccuracy { get; set; } = 0;
}
public class GetAllGovernoratesQuery : RegionListQuery { }
public class GetGovernoratesByCountryQuery : RegionListQuery { public int Adm0Gid { get; set; } }

public class GetGovernorateByIdHandler(IAdm1Repository repository)
{
    public async Task<Adm1Dto?> HandleAsync(GetGovernorateByIdQuery query, CancellationToken ct)
    {
        var e = await repository.GetByIdAsync(query.Gid, false, ct);
        if (e is null) return null;

        JsonElement? geometry = null;
        if (query.GeometryAccuracy > 0)
        {
            var result = await repository.GetGeometryGeoJsonAsync(query.Gid, query.GeometryAccuracy, ct);
            geometry = GeoJsonSerializationHelper.ToJsonElement(result?.GeoJson);
        }

        return GovernorateQueryMapper.Map(e, geometry);
    }
}

public class GetAllGovernoratesHandler(IAdm1Repository repository)
{
    public async Task<List<Adm1Dto>> HandleAsync(GetAllGovernoratesQuery query, CancellationToken ct)
    {
        var list = await repository.GetAllAsync(query, ct);
        var geometries = await GovernorateQueryMapper.GetGeometryMapAsync(repository, list, query, ct);
        return list.ConvertAll(e => GovernorateQueryMapper.Map(e, geometries.GetValueOrDefault(e.Gid)));
    }
}

public class GetGovernoratesByCountryHandler(IAdm1Repository repository)
{
    public async Task<List<Adm1Dto>> HandleAsync(GetGovernoratesByCountryQuery query, CancellationToken ct)
    {
        var list = await repository.GetByAdm0Async(query.Adm0Gid, query, ct);
        var geometries = await GovernorateQueryMapper.GetGeometryMapAsync(repository, list, query, ct);
        return list.ConvertAll(e => GovernorateQueryMapper.Map(e, geometries.GetValueOrDefault(e.Gid)));
    }
}

internal static class GovernorateQueryMapper
{
    internal static async Task<Dictionary<int, JsonElement?>> GetGeometryMapAsync(
        IAdm1Repository repository,
        List<Adm1> list,
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

    internal static Adm1Dto Map(Adm1 e, JsonElement? geometryGeoJson)
    {
        return new Adm1Dto
        {
            Gid = e.Gid,
            Adm0Gid = e.Adm0Gid,
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
