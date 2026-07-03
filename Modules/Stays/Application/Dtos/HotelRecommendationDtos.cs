namespace Glinter.Modules.Stays.Application.Dtos;

public class HotelRecommendationRequest
{
    public int? BudgetLevel { get; set; }
    public List<HotelRecommendationCategoryPreference> ExperienceCategories { get; set; } = [];
    public List<string> RequestedAmenities { get; set; } = [];
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public int? Limit { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class HotelRecommendationCategoryPreference
{
    public string Category { get; set; } = string.Empty;
    public double? Weight { get; set; }
}

public class NaturalLanguageHotelRecommendationRequest
{
    public string Text { get; set; } = string.Empty;
    public int? Limit { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class HotelRecommendationResponse
{
    public HotelRecommendationPreferences Preferences { get; set; } = new();
    // Compatibility alias for TotalMatchingCandidates.
    public int TotalCandidates { get; set; }
    public int TotalMatchingCandidates { get; set; }
    public int EvaluatedCandidates { get; set; }
    public int ReturnedCount { get; set; }
    public int ReturnedRecommendations => ReturnedCount;
    public List<HotelRecommendationItemResponse> Items { get; set; } = [];
}

public class NaturalLanguageHotelRecommendationResponse : HotelRecommendationResponse
{
    public string InputText { get; set; } = string.Empty;
    public string? ClassificationNotes { get; set; }
}

public class HotelRecommendationPreferences
{
    public int? BudgetLevel { get; set; }
    public string? BudgetLabel { get; set; }
    public List<WeightedExperienceCategoryPreference> ExperienceCategories { get; set; } = [];
    public List<string> RequestedAmenities { get; set; } = [];
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public int Limit { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public double? ClassificationConfidence { get; set; }
    public string? Notes { get; set; }
    public string? RegionName { get; set; }
    public string? ResolvedRegionName { get; set; }
}

public class WeightedExperienceCategoryPreference
{
    public string Category { get; set; } = string.Empty;
    public double Weight { get; set; }
}

public class HotelRecommendationItemResponse
{
    public int Ranking { get; set; }
    public int HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public string? LocationSummaryDescription { get; set; }
    public int? BudgetLevel { get; set; }
    public string? BudgetLabel { get; set; }
    public decimal? Rating { get; set; }
    public int? Reviews { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? Adm0Gid { get; set; }
    public int? Adm1Gid { get; set; }
    public int? Adm2Gid { get; set; }
    public int? Adm3Gid { get; set; }
    public HotelRecommendationRegionResponse? Region { get; set; }
    public string? GoogleMapsLink { get; set; }
    public string? Website { get; set; }
    public string? PhoneInternational { get; set; }
    public double FinalScore { get; set; }
    public HotelRecommendationScoreBreakdown Scores { get; set; } = new();
    public List<string> Amenities { get; set; } = [];
    public List<string> MatchedAmenities { get; set; } = [];
    public List<NearbyExperienceSummaryResponse> NearbyExperiences { get; set; } = [];
    public HotelRecommendationExplanationResponse Explanation { get; set; } = new();
}

public class HotelRecommendationRegionResponse
{
    public string? CountryNameEn { get; set; }
    public string? CountryNameAr { get; set; }
    public string? GovernorateNameEn { get; set; }
    public string? GovernorateNameAr { get; set; }
    public string? DistrictNameEn { get; set; }
    public string? DistrictNameAr { get; set; }
    public string? NeighbourhoodNameEn { get; set; }
    public string? NeighbourhoodNameAr { get; set; }
    public string? DisplayName { get; set; }
}

public class HotelRecommendationScoreBreakdown
{
    public double? InterestProximityScore { get; set; }
    public double? BudgetMatchScore { get; set; }
    public double? HotelQualityScore { get; set; }
    public double? AmenityMatchScore { get; set; }
}

public class NearbyExperienceSummaryResponse
{
    public int ExperienceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public decimal? Rating { get; set; }
    public int? Reviews { get; set; }
}

public class HotelRecommendationExplanationResponse
{
    public string ShortExplanation { get; set; } = string.Empty;
    public List<string> Reasons { get; set; } = [];
    public List<string> BestFor { get; set; } = [];
    public bool IsAiGenerated { get; set; }
}
