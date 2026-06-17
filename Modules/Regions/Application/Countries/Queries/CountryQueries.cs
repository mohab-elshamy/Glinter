using Glinter.Modules.Regions.Application.Abstractions;
using Glinter.Modules.Regions.Application.Common.Mapping;
using Glinter.Modules.Regions.Application.DTOs;
using Glinter.Modules.Regions.Domain.Entities;
using System.Text.Json;

namespace Glinter.Modules.Regions.Application.Countries.Queries;

public class GetCountryByIdQuery
{
    public int Gid { get; set; }
    public int GeometryAccuracy { get; set; } = 0;
}

public class GetAllCountriesQuery : RegionListQuery { }

public class GetCountryByIdHandler(IAdm0Repository repository)
{
    public async Task<Adm0Dto?> HandleAsync(GetCountryByIdQuery query, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(query.Gid, false, ct);
        if (entity is null) return null;

        JsonElement? geometry = null;
        if (query.GeometryAccuracy > 0)
        {
            var result = await repository.GetGeometryGeoJsonAsync(query.Gid, query.GeometryAccuracy, ct);
            geometry = GeoJsonSerializationHelper.ToJsonElement(result?.GeoJson);
        }

        return CountryQueryMapper.Map(entity, geometry);
    }
}

public class GetAllCountriesHandler(IAdm0Repository repository)
{
    public async Task<List<Adm0Dto>> HandleAsync(GetAllCountriesQuery query, CancellationToken ct)
    {
        var list = await repository.GetAllAsync(query, ct);
        var geometries = new Dictionary<int, JsonElement?>();
        if (query.GeometryAccuracy > 0)
        {
            var geoJsonRows = await repository.GetGeometryGeoJsonByIdsAsync(
                list.Select(x => x.Gid).ToArray(),
                query.GeometryAccuracy,
                ct);
            geometries = RegionGeometryMappingHelper.ToJsonByGid(geoJsonRows);
        }

        return list.ConvertAll(e =>
            CountryQueryMapper.Map(e, geometries.GetValueOrDefault(e.Gid)));
    }
}

internal static class CountryQueryMapper
{
    internal static Adm0Dto Map(Adm0 e, JsonElement? geometryGeoJson)
    {
        return new Adm0Dto
        {
            Gid = e.Gid,
            NameEn = e.NameEn,
            NameAr = e.NameAr,
            Pcode = e.Pcode,
            ImageUrl = e.ImageUrl,
            FlagUrl = e.FlagUrl,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            GeometryGeoJson = geometryGeoJson,
        };
    }
}
