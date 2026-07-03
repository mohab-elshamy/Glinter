namespace Glinter.Modules.Itineraries.Domain.Entities;

public sealed class SavedItinerary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public decimal? EstimatedTotalCost { get; set; }
    public string? Currency { get; set; }
    public string? PlannerExplanation { get; set; }
    public string? WarningsJson { get; set; }
    public double? RecommendationScore { get; set; }
    public double? TotalDistanceKm { get; set; }
    public int? TotalTravelMinutes { get; set; }
    public string? Pace { get; set; }
    public string? TravelMode { get; set; }
    public string? FallbackTravelMode { get; set; }
    public double? OriginLatitude { get; set; }
    public double? OriginLongitude { get; set; }
    public string? OriginLabel { get; set; }
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
    public string? WeatherLocation { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<SavedItineraryItem> Items { get; set; } = [];
}
