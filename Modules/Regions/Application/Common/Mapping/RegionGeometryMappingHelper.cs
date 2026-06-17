using System.Text.Json;
using Glinter.Modules.Regions.Application.DTOs;

namespace Glinter.Modules.Regions.Application.Common.Mapping;

public static class RegionGeometryMappingHelper
{
    public static Dictionary<int, JsonElement?> ToJsonByGid(IEnumerable<RegionGeometryGeoJson> geometries)
        => geometries.ToDictionary(
            x => x.Gid,
            x => GeoJsonSerializationHelper.ToJsonElement(x.GeoJson));
}
