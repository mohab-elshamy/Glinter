namespace Glinter.Modules.Itineraries.Application.Options;

public class ItineraryPlanningOptions
{
    public const string SectionName = "ItineraryPlanning";

    public string OpenRouteServiceApiKey { get; set; } = string.Empty;

    public string OpenRouteServiceBaseUrl { get; set; } = "https://api.openrouteservice.org";

    public string OpenTripPlannerBaseUrl { get; set; } = "http://localhost:8040";

    public string OpenTripPlannerGraphQlPath { get; set; } = "/otp/gtfs/v1";

    public int RoutingRequestTimeoutSeconds { get; set; } = 20;

    public double FallbackWalkingKmh { get; set; } = 4.5;

    public double FallbackDrivingKmh { get; set; } = 25;

    public double FallbackCyclingKmh { get; set; } = 12;

    public double FallbackTransitKmh { get; set; } = 18;

    public int DefaultMaxStops { get; set; } = 5;

    public int MaxStops { get; set; } = 10;

    public int DefaultCandidateLimit { get; set; } = 30;

    public int MaxCandidateLimit { get; set; } = 50;

    public int NaturalLanguageMaxCharacters { get; set; } = 1500;

    public int MaxSelectedCategories { get; set; } = 5;

    public int MaxTravelers { get; set; } = 50;

    public int MinimumMealBreakMinutes { get; set; } = 45;

    public string GroqApiKey { get; set; } = string.Empty;

    public string GroqModel { get; set; } = "llama-3.1-8b-instant";

    public string GroqBaseUrl { get; set; } = "https://api.groq.com/openai/v1";

    public int GroqRequestTimeoutSeconds { get; set; } = 45;

    public bool GroqRetryWithoutResponseFormatOnBadRequest { get; set; } = true;

    public int GroqErrorBodyLogCharacters { get; set; } = 800;

    public int GroqClassificationMaxTokens { get; set; } = 700;

    public ItineraryPlanningRateLimitingOptions RateLimiting { get; set; } = new();
}

public class ItineraryPlanningRateLimitingOptions
{
    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;
}
