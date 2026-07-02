using Glinter.Modules.Itineraries.Application.Abstractions;
using Glinter.Modules.Itineraries.Application.Options;
using Glinter.Modules.Itineraries.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Glinter.Modules.Itineraries.Infrastructure.Routing;

public class ItineraryRoutePlanner(
    OpenRouteServiceClient openRouteServiceClient,
    OpenTripPlannerClient openTripPlannerClient,
    IOptions<ItineraryPlanningOptions> options,
    ILogger<ItineraryRoutePlanner> logger) : IItineraryRoutePlanner
{
    private readonly ItineraryPlanningOptions _options = options.Value;

    public async Task<ItineraryRouteResult> GetRouteAsync(
        ItineraryRouteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Mode == ItineraryTravelMode.PublicTransit)
            {
                var transit = await openTripPlannerClient.TryGetRouteAsync(request, cancellationToken);
                if (transit is not null)
                {
                    return transit;
                }

                var fallbackRequest = CloneForMode(request, request.FallbackMode);
                var fallback = await TryOpenRouteServiceAsync(fallbackRequest, cancellationToken);
                if (fallback is not null)
                {
                    fallback.Warnings.Add("Public transit route was unavailable; fallback travel mode was used.");
                    return fallback;
                }
            }
            else
            {
                var route = await TryOpenRouteServiceAsync(request, cancellationToken);
                if (route is not null)
                {
                    return route;
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Itinerary route provider failed. Falling back to straight-line estimate.");
        }

        return BuildFallbackEstimate(request);
    }

    private async Task<ItineraryRouteResult?> TryOpenRouteServiceAsync(
        ItineraryRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Mode == ItineraryTravelMode.PublicTransit)
        {
            return null;
        }

        return await openRouteServiceClient.TryGetRouteAsync(request, cancellationToken);
    }

    private ItineraryRouteResult BuildFallbackEstimate(ItineraryRouteRequest request)
    {
        var mode = request.Mode == ItineraryTravelMode.PublicTransit
            ? request.FallbackMode
            : request.Mode;

        var distanceKm = HaversineKm(
            request.From.Latitude,
            request.From.Longitude,
            request.To.Latitude,
            request.To.Longitude);

        var speed = mode switch
        {
            ItineraryTravelMode.Driving => Math.Max(1, _options.FallbackDrivingKmh),
            ItineraryTravelMode.Cycling => Math.Max(1, _options.FallbackCyclingKmh),
            ItineraryTravelMode.PublicTransit => Math.Max(1, _options.FallbackTransitKmh),
            _ => Math.Max(1, _options.FallbackWalkingKmh)
        };

        return new ItineraryRouteResult
        {
            Mode = mode,
            Provider = ItineraryRouteProvider.FallbackEstimate,
            DistanceKm = Round(distanceKm),
            DurationMinutes = Math.Max(1, (int)Math.Ceiling(distanceKm / speed * 60)),
            Geometry = RouteGeometryBuilder.FromStraightLine(
                request.From.Latitude,
                request.From.Longitude,
                request.To.Latitude,
                request.To.Longitude),
            Warnings =
            [
                "Route provider was unavailable; straight-line travel estimate was used."
            ]
        };
    }

    private static ItineraryRouteRequest CloneForMode(ItineraryRouteRequest request, ItineraryTravelMode mode) =>
        new()
        {
            From = request.From,
            To = request.To,
            Mode = mode,
            FallbackMode = request.FallbackMode,
            DepartureLocal = request.DepartureLocal,
            AvoidLongWalking = request.AvoidLongWalking
        };

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radiusKm = 6371.0088;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var rLat1 = DegreesToRadians(lat1);
        var rLat2 = DegreesToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return radiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
