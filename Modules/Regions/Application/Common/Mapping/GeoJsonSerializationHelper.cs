using System.Text.Json;

namespace Glinter.Modules.Regions.Application.Common.Mapping;

public static class GeoJsonSerializationHelper
{
    public static JsonElement? ToJsonElement(string? geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(geoJson);
        return document.RootElement.Clone();
    }
}