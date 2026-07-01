namespace Glinter.Modules.Stays.Application.Options;

public class HotelRecommendationOptions
{
    public const string SectionName = "HotelRecommendations";

    public string GroqApiKey { get; set; } = string.Empty;

    public string GroqModel { get; set; } = "llama-3.1-8b-instant";

    public string GroqBaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    public int GroqRequestTimeoutSeconds { get; set; } = 45;

    public bool GroqRetryWithoutResponseFormatOnBadRequest { get; set; } = true;

    public int GroqErrorBodyLogCharacters { get; set; } = 800;

    public int GroqClassificationMaxTokens { get; set; } = 500;

    public int GroqExplanationMaxTokens { get; set; } = 750;

    public int GroqMaxExplanationItems { get; set; } = 8;

    public int GroqRetryMaxExplanationItems { get; set; } = 3;

    public int GroqMaxDescriptionCharacters { get; set; } = 220;

    public int GroqMaxLocationSummaryCharacters { get; set; } = 180;

    public int GroqMaxAmenitiesPerHotel { get; set; } = 6;

    public int GroqMaxNearbyExperiencesPerHotel { get; set; } = 3;

    public int DefaultLimit { get; set; } = 10;

    public int MaxLimit { get; set; } = 25;

    public int MaxCandidateHotels { get; set; } = 300;

    public int MaxExperiencesPerCategory { get; set; } = 750;

    public int NearestExperiencesPerCategory { get; set; } = 5;

    public double MaxUsefulDistanceKm { get; set; } = 20;

    public int MinLocalPriceSampleSize { get; set; } = 20;
}
