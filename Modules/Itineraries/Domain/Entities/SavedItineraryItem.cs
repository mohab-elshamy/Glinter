namespace Glinter.Modules.Itineraries.Domain.Entities;

public sealed class SavedItineraryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItineraryId { get; set; }
    public int DayNumber { get; set; }
    public int SortOrder { get; set; }
    public string EntityType { get; set; } = "Experience";
    public int? EntityId { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? Explanation { get; set; }
    public string? TravelModeFromPrevious { get; set; }
    public double? DistanceKmFromPrevious { get; set; }
    public int? TravelDurationMinutesFromPrevious { get; set; }
    public SavedItinerary Itinerary { get; set; } = null!;
}
