using Glinter.Modules.Itineraries.Application.Dtos;

namespace Glinter.Modules.Itineraries.Infrastructure.Routing;

internal static class RouteGeometryBuilder
{
    public static ItineraryLegGeometryResponse FromStraightLine(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude) =>
        new()
        {
            Coordinates =
            [
                [Round(fromLongitude), Round(fromLatitude)],
                [Round(toLongitude), Round(toLatitude)]
            ]
        };

    public static ItineraryLegGeometryResponse? FromEncodedPolyline(string? encodedPolyline)
    {
        if (string.IsNullOrWhiteSpace(encodedPolyline))
        {
            return null;
        }

        var coordinates = DecodePolyline(encodedPolyline)
            .Select(point => new[] { Round(point.Longitude), Round(point.Latitude) })
            .ToList();

        return coordinates.Count < 2
            ? null
            : new ItineraryLegGeometryResponse { Coordinates = coordinates };
    }

    public static ItineraryLegGeometryResponse? CombineEncodedPolylines(IEnumerable<string?> encodedPolylines)
    {
        var coordinates = new List<double[]>();
        foreach (var encodedPolyline in encodedPolylines.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var geometry = FromEncodedPolyline(encodedPolyline);
            if (geometry is null)
            {
                continue;
            }

            foreach (var coordinate in geometry.Coordinates)
            {
                if (coordinates.Count == 0 ||
                    Math.Abs(coordinates[^1][0] - coordinate[0]) > 0.000001 ||
                    Math.Abs(coordinates[^1][1] - coordinate[1]) > 0.000001)
                {
                    coordinates.Add(coordinate);
                }
            }
        }

        return coordinates.Count < 2
            ? null
            : new ItineraryLegGeometryResponse { Coordinates = coordinates };
    }

    private static IEnumerable<RoutePoint> DecodePolyline(string encodedPolyline)
    {
        var index = 0;
        var latitude = 0;
        var longitude = 0;

        while (index < encodedPolyline.Length)
        {
            latitude += DecodeNextValue(encodedPolyline, ref index);
            longitude += DecodeNextValue(encodedPolyline, ref index);

            yield return new RoutePoint(latitude / 100000.0, longitude / 100000.0);
        }
    }

    private static int DecodeNextValue(string encodedPolyline, ref int index)
    {
        var result = 0;
        var shift = 0;
        int b;

        do
        {
            b = encodedPolyline[index++] - 63;
            result |= (b & 0x1f) << shift;
            shift += 5;
        }
        while (b >= 0x20 && index < encodedPolyline.Length);

        return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
    }

    private static double Round(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed record RoutePoint(double Latitude, double Longitude);
}
