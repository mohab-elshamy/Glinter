using Glinter.Modules.Experiences.Application.Dtos;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Itineraries.Domain.Enums;

namespace Glinter.Modules.Itineraries.Application.Dtos;

public class ItineraryPlanRequest
{
    public ItineraryPointRequest Start { get; set; } = new();
    public ItineraryPointRequest? End { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? DayStartLocal { get; set; }
    public TimeOnly? DayEndLocal { get; set; }
    public ItineraryTravelMode TravelMode { get; set; } = ItineraryTravelMode.PublicTransit;
    public ItineraryTravelMode FallbackTravelMode { get; set; } = ItineraryTravelMode.Walking;
    public ItineraryPace Pace { get; set; } = ItineraryPace.Balanced;
    public List<ItineraryCategoryPreference> Categories { get; set; } = [];
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public int? MaxStops { get; set; }
    public int? CandidateLimit { get; set; }
    public int? GuestsCount { get; set; }
    public ExperienceCrowdPreference? CrowdPreference { get; set; }
    public bool IncludeMealBreaks { get; set; }
    public bool ReturnToStart { get; set; } = true;
    public bool AvoidLongWalking { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class NaturalLanguageItineraryPlanRequest
{
    public string Text { get; set; } = string.Empty;
    public ItineraryPointRequest Start { get; set; } = new();
    public ItineraryPointRequest? End { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? DayStartLocal { get; set; }
    public TimeOnly? DayEndLocal { get; set; }
    public ItineraryTravelMode? TravelMode { get; set; }
    public ItineraryTravelMode? FallbackTravelMode { get; set; }
    public ItineraryPace? Pace { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public int? MaxStops { get; set; }
    public int? CandidateLimit { get; set; }
    public int? GuestsCount { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class ItineraryPointRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Label { get; set; }
}

public class ItineraryCategoryPreference
{
    public string Category { get; set; } = string.Empty;
    public double? Weight { get; set; }
}

public class ItineraryPlanResponse
{
    public DateOnly Date { get; set; }
    public ItineraryTravelMode TravelMode { get; set; }
    public ItineraryTravelMode FallbackTravelMode { get; set; }
    public ItineraryPace Pace { get; set; }
    public int TotalCandidateExperiences { get; set; }
    public int SelectedStopsCount { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalTravelMinutes { get; set; }
    public double TotalDistanceKm { get; set; }
    public double Score { get; set; }
    public List<string> Warnings { get; set; } = [];
    public List<ItineraryStopResponse> Stops { get; set; } = [];
    public List<ItineraryLegResponse> Legs { get; set; } = [];
    public ItineraryExplanationResponse Explanation { get; set; } = new();
}

public class NaturalLanguageItineraryPlanResponse
{
    public string InputText { get; set; } = string.Empty;
    public ItineraryPlanRequest InterpretedRequest { get; set; } = new();
    public ItineraryPlanClassificationResponse Classification { get; set; } = new();
    public ItineraryPlanResponse Itinerary { get; set; } = new();
}

public class ItineraryPlanClassificationResponse
{
    public bool IsAiGenerated { get; set; }
    public double? Confidence { get; set; }
    public string? Notes { get; set; }
}

public class ItineraryStopResponse
{
    public int Order { get; set; }
    public ItineraryStopType Type { get; set; }
    public int? ExperienceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ExperienceCategory? Category { get; set; }
    public string? RegionDisplayName { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ArrivalLocal { get; set; } = string.Empty;
    public string DepartureLocal { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public double? RecommendationScore { get; set; }
    public string? PrimaryImage { get; set; }
    public List<string> Notes { get; set; } = [];
}

public class ItineraryLegResponse
{
    public int FromOrder { get; set; }
    public int ToOrder { get; set; }
    public ItineraryTravelMode Mode { get; set; }
    public ItineraryRouteProvider Provider { get; set; }
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public ItineraryLegGeometryResponse? Geometry { get; set; }
    public List<string> Steps { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class ItineraryLegGeometryResponse
{
    public string Type { get; set; } = "LineString";
    public List<double[]> Coordinates { get; set; } = [];
}

public class ItineraryExplanationResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = [];
    public bool IsAiGenerated { get; set; }
}

public class ItineraryPlanPreferences
{
    public List<ItineraryCategoryPreference> Categories { get; set; } = [];
    public TimeOnly? DayStartLocal { get; set; }
    public TimeOnly? DayEndLocal { get; set; }
    public ItineraryTravelMode? TravelMode { get; set; }
    public ItineraryTravelMode? FallbackTravelMode { get; set; }
    public ItineraryPace? Pace { get; set; }
    public int? MaxStops { get; set; }
    public bool? IncludeMealBreaks { get; set; }
    public bool? ReturnToStart { get; set; }
    public bool? AvoidLongWalking { get; set; }
    public ExperienceCrowdPreference? CrowdPreference { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public double? ClassificationConfidence { get; set; }
    public string? Notes { get; set; }
}
