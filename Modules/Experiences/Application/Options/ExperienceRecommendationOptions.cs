namespace Glinter.Modules.Experiences.Application.Options;

public class ExperienceRecommendationOptions
{
    public const string SectionName = "ExperienceRecommendations";

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

    public int DefaultLimit { get; set; } = 10;

    public int DefaultItineraryLimit { get; set; } = 30;

    public int MaxLimit { get; set; } = 40;

    public int MaxCandidateExperiences { get; set; } = 600;

    public int NaturalLanguageMaxCharacters { get; set; } = 1000;

    public int MaxRequestedCategories { get; set; } = 5;

    public int MaxRequestedAmenities { get; set; } = 20;

    public double MaxUsefulDistanceKm { get; set; } = 20;

    public int DefaultHistoricalDurationMinutes { get; set; } = 90;

    public int DefaultNatureDurationMinutes { get; set; } = 120;

    public int DefaultShoppingDurationMinutes { get; set; } = 90;

    public int DefaultNightlifeDurationMinutes { get; set; } = 120;

    public int DefaultDiningDurationMinutes { get; set; } = 75;

    public ExperienceRecommendationRateLimitingOptions RateLimiting { get; set; } = new();
}

public class ExperienceRecommendationRateLimitingOptions
{
    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}
