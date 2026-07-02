using Glinter.Modules.Itineraries.Application.Dtos;
using Glinter.Modules.Itineraries.Domain.Enums;

namespace Glinter.Modules.Itineraries.Application.Abstractions;

public interface IItineraryRoutePlanner
{
    Task<ItineraryRouteResult> GetRouteAsync(
        ItineraryRouteRequest request,
        CancellationToken cancellationToken);
}

public sealed class ItineraryRouteRequest
{
    public ItineraryPointRequest From { get; set; } = new();
    public ItineraryPointRequest To { get; set; } = new();
    public ItineraryTravelMode Mode { get; set; }
    public ItineraryTravelMode FallbackMode { get; set; }
    public DateTime DepartureLocal { get; set; }
    public bool AvoidLongWalking { get; set; }
}

public sealed class ItineraryRouteResult
{
    public ItineraryTravelMode Mode { get; set; }
    public ItineraryRouteProvider Provider { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public ItineraryLegGeometryResponse? Geometry { get; set; }
    public List<string> Steps { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
