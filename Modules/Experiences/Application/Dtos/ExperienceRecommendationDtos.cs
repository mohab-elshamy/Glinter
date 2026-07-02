using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Dtos;

public class ExperienceRecommendationRequest
{
    public List<ExperienceRecommendationCategoryPreference> Categories { get; set; } = [];
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public DateTime? VisitAtLocal { get; set; }
    public int? GuestsCount { get; set; }
    public ExperienceCrowdPreference? CrowdPreference { get; set; }
    public bool BookableOnly { get; set; }
    public bool ForItinerary { get; set; }
    public int? Limit { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class NaturalLanguageExperienceRecommendationRequest
{
    public string Text { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public DateTime? VisitAtLocal { get; set; }
    public int? GuestsCount { get; set; }
    public bool? ForItinerary { get; set; }
    public int? Limit { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class ExperienceRecommendationCategoryPreference
{
    public string Category { get; set; } = string.Empty;
    public double? Weight { get; set; }
}

public class ExperienceRecommendationResponse
{
    public ExperienceRecommendationPreferences Preferences { get; set; } = new();
    public int TotalCandidates { get; set; }
    public int ReturnedCount { get; set; }
    public List<ExperienceRecommendationItemResponse> Items { get; set; } = [];
}

public class NaturalLanguageExperienceRecommendationResponse : ExperienceRecommendationResponse
{
    public string InputText { get; set; } = string.Empty;
    public string? ClassificationNotes { get; set; }
}

public class ExperienceRecommendationPreferences
{
    public List<WeightedExperienceRecommendationCategory> Categories { get; set; } = [];
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public DateTime? VisitAtLocal { get; set; }
    public int? GuestsCount { get; set; }
    public ExperienceCrowdPreference? CrowdPreference { get; set; }
    public bool BookableOnly { get; set; }
    public bool ForItinerary { get; set; }
    public int Limit { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public double? ClassificationConfidence { get; set; }
    public string? Notes { get; set; }
}

public class WeightedExperienceRecommendationCategory
{
    public ExperienceCategory Category { get; set; }
    public double Weight { get; set; }
}

public enum ExperienceCrowdPreference
{
    Quiet = 1,
    Balanced = 2,
    Lively = 3
}

public class ExperienceRecommendationItemResponse
{
    public int Ranking { get; set; }
    public int ExperienceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ExperienceCategory Category { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public string? RegionDisplayName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? DistanceKm { get; set; }
    public decimal? Rating { get; set; }
    public int? Reviews { get; set; }
    public string? PriceRange { get; set; }
    public decimal? StartingPricePerPerson { get; set; }
    public string? PrimaryImage { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string DurationSource { get; set; } = "DefaultEstimate";
    public ExperienceRecommendedVisitWindowResponse? RecommendedVisitWindow { get; set; }
    public ExperienceOpeningWindowResponse? OpeningWindow { get; set; }
    public bool OpenHoursDataAvailable { get; set; }
    public bool PopularTimesDataAvailable { get; set; }
    public bool AvailabilityDataAvailable { get; set; }
    public ExperienceNextAvailableSlotResponse? NextAvailableSlot { get; set; }
    public ExperienceRoutingHintsResponse RoutingHints { get; set; } = new();
    public double FinalScore { get; set; }
    public ExperienceRecommendationScoreBreakdown Scores { get; set; } = new();
    public List<string> Amenities { get; set; } = [];
    public ExperienceRecommendationExplanationResponse Explanation { get; set; } = new();
}

public class ExperienceRecommendedVisitWindowResponse
{
    public string? StartLocal { get; set; }
    public string? EndLocal { get; set; }
}

public class ExperienceOpeningWindowResponse
{
    public string? OpensAt { get; set; }
    public string? ClosesAt { get; set; }
}

public class ExperienceNextAvailableSlotResponse
{
    public Guid AvailabilityId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int RemainingCapacity { get; set; }
    public decimal PricePerPerson { get; set; }
}

public class ExperienceRoutingHintsResponse
{
    public bool MustVisitAtFixedTime { get; set; }
    public bool RequiresBooking { get; set; }
    public string? ClusterKey { get; set; }
    public string? TimeOfDayPreference { get; set; }
}

public class ExperienceRecommendationScoreBreakdown
{
    public double? CategoryMatchScore { get; set; }
    public double? DistanceScore { get; set; }
    public double? QualityScore { get; set; }
    public double? TimingOpenScore { get; set; }
    public double? CrowdPreferenceScore { get; set; }
    public double? AvailabilityScore { get; set; }
    public double? ContentCompletenessScore { get; set; }
}

public class ExperienceRecommendationExplanationResponse
{
    public string ShortExplanation { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = [];
    public List<string> BestFor { get; set; } = [];
    public bool IsAiGenerated { get; set; }
}
