namespace Glinter.Modules.Itineraries.Application.Dtos;

public sealed class SaveItineraryRequest
{
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
    public List<string> Warnings { get; set; } = [];
    public double? RecommendationScore { get; set; }
    public double? TotalDistanceKm { get; set; }
    public int? TotalTravelMinutes { get; set; }
    public string? Pace { get; set; }
    public string? TravelMode { get; set; }
    public string? FallbackTravelMode { get; set; }
    public ItineraryPointRequest? Origin { get; set; }
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
    public string? WeatherLocation { get; set; }
    public List<SaveItineraryItemRequest> Items { get; set; } = [];
}

public sealed class UpdateSavedItineraryRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public decimal? EstimatedTotalCost { get; set; }
    public string? Currency { get; set; }
    public DateTime ExpectedUpdatedAtUtc { get; set; }
}

public sealed class SaveItineraryItemRequest
{
    public Guid? Id { get; set; }
    public int DayNumber { get; set; }
    public int SortOrder { get; set; }
    public string EntityType { get; set; } = "Experience";
    public int? EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? Explanation { get; set; }
    public string? Category { get; set; }
    public decimal? Rating { get; set; }
    public string? ImageUrl { get; set; }
    public string? TravelModeFromPrevious { get; set; }
    public string? RouteProviderFromPrevious { get; set; }
    public ItineraryLegGeometryResponse? RouteGeometryFromPrevious { get; set; }
    public List<string> RouteInstructionsFromPrevious { get; set; } = [];
    public List<string> RouteWarningsFromPrevious { get; set; } = [];
    public double? DistanceKmFromPrevious { get; set; }
    public int? TravelDurationMinutesFromPrevious { get; set; }
}

public sealed class ReplaceItineraryItemsRequest
{
    public DateTime ExpectedUpdatedAtUtc { get; set; }
    public List<SaveItineraryItemRequest> Items { get; set; } = [];
}

public sealed class SavedItineraryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public decimal? EstimatedTotalCost { get; set; }
    public string? Currency { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public string? PlannerExplanation { get; set; }
    public List<string> Warnings { get; set; } = [];
    public double? RecommendationScore { get; set; }
    public double? TotalDistanceKm { get; set; }
    public int? TotalTravelMinutes { get; set; }
    public string? Pace { get; set; }
    public string? TravelMode { get; set; }
    public string? FallbackTravelMode { get; set; }
    public ItineraryPointRequest? Origin { get; set; }
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
    public string? WeatherLocation { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<SavedItineraryItemResponse> Items { get; set; } = [];
}

public sealed class SavedItineraryItemResponse
{
    public Guid Id { get; set; }
    public int DayNumber { get; set; }
    public int SortOrder { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public decimal? EstimatedCost { get; set; }
    public string? Explanation { get; set; }
    public string? Category { get; set; }
    public decimal? Rating { get; set; }
    public string? ImageUrl { get; set; }
    public string? TravelModeFromPrevious { get; set; }
    public string? RouteProviderFromPrevious { get; set; }
    public ItineraryLegGeometryResponse? RouteGeometryFromPrevious { get; set; }
    public List<string> RouteInstructionsFromPrevious { get; set; } = [];
    public List<string> RouteWarningsFromPrevious { get; set; } = [];
    public double? DistanceKmFromPrevious { get; set; }
    public int? TravelDurationMinutesFromPrevious { get; set; }
}
