using System.Text.Json;
using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class ItineraryEnhancementTests : ApiTestBase
{
    public ItineraryEnhancementTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Multi_day_plan_is_one_response_with_budget_origin_and_unique_experiences()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/itineraries/plan",
            body: new
            {
                origin = new { latitude = 31.2, longitude = 29.9, label = "Alexandria start" },
                start = new { latitude = 31.2, longitude = 29.9 },
                date = start,
                startDate = start,
                endDate = start.AddDays(2),
                totalBudget = 100,
                currency = "USD",
                categories = Array.Empty<object>(),
                maxStops = 2,
                candidateLimit = 30
            });
        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        Assert.Equal(3, json.RootElement.GetProperty("days").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("budgetApplied").GetBoolean());
        Assert.Equal("USD", json.RootElement.GetProperty("currency").GetString());
        Assert.Contains("Alexandria", json.RootElement.GetProperty("originSource").GetString());
        var experienceIds = json.RootElement.GetProperty("days").EnumerateArray()
            .SelectMany(day => day.GetProperty("stops").EnumerateArray())
            .Where(stop => stop.TryGetProperty("experienceId", out var id) && id.ValueKind == JsonValueKind.Number)
            .Select(stop => stop.GetProperty("experienceId").GetInt32())
            .ToArray();
        Assert.Equal(experienceIds.Distinct().Count(), experienceIds.Length);
    }

    [Fact]
    public async Task Unsupported_trip_length_is_rejected_consistently()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/itineraries/plan",
            body: new
            {
                origin = new { latitude = 30.04, longitude = 31.23 },
                start = new { latitude = 30.04, longitude = 31.23 },
                startDate = start,
                endDate = start.AddDays(14),
                categories = Array.Empty<object>()
            });
        await AssertProblemAsync(response, 400, "invalid_itinerary_request");
    }

    [Fact]
    public async Task Rich_saved_itinerary_round_trips_route_and_planner_metadata()
    {
        var traveler = await CreateUserAsync("Traveler", "rich-itinerary");
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var save = await SendAsync(
            HttpMethod.Post,
            "/api/itineraries",
            traveler.Token,
            new
            {
                title = "Rich saved trip",
                destination = "Luxor",
                startDate = start,
                endDate = start,
                preferredLanguage = "en",
                estimatedTotalCost = 40,
                currency = "USD",
                plannerExplanation = "Deterministic plan explanation",
                warnings = new[] { "A routing fallback was used." },
                recommendationScore = 88,
                totalDistanceKm = 4.2,
                totalTravelMinutes = 35,
                pace = "Balanced",
                travelMode = "PublicTransit",
                fallbackTravelMode = "Walking",
                origin = new { latitude = 25.68, longitude = 32.64, label = "Luxor start" },
                weatherLatitude = 25.68,
                weatherLongitude = 32.64,
                weatherLocation = "Luxor",
                items = new[]
                {
                    new
                    {
                        dayNumber = 1,
                        sortOrder = 1,
                        entityType = "Experience",
                        entityId = 123,
                        name = "Temple",
                        latitude = 25.7,
                        longitude = 32.6,
                        category = "Historical",
                        rating = 4.8,
                        imageUrl = "https://example.test/temple.jpg",
                        travelModeFromPrevious = "Walking",
                        routeProviderFromPrevious = "FallbackEstimate",
                        routeGeometryFromPrevious = new
                        {
                            type = "LineString",
                            coordinates = new[] { new[] { 32.64, 25.68 }, new[] { 32.6, 25.7 } }
                        },
                        routeInstructionsFromPrevious = new[] { "Walk east" },
                        routeWarningsFromPrevious = new[] { "Fallback" },
                        distanceKmFromPrevious = 1.2,
                        travelDurationMinutesFromPrevious = 15
                    }
                }
            });
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        using var saveJson = await ReadJsonAsync(save);
        var id = saveJson.RootElement.GetProperty("id").GetGuid();

        var get = await SendAsync(HttpMethod.Get, $"/api/itineraries/{id}", traveler.Token);
        get.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(get);
        Assert.Equal("Deterministic plan explanation", json.RootElement.GetProperty("plannerExplanation").GetString());
        Assert.Equal(4.2, json.RootElement.GetProperty("totalDistanceKm").GetDouble());
        var item = json.RootElement.GetProperty("items")[0];
        Assert.Equal("FallbackEstimate", item.GetProperty("routeProviderFromPrevious").GetString());
        Assert.Equal(2, item.GetProperty("routeGeometryFromPrevious").GetProperty("coordinates").GetArrayLength());
        Assert.Equal("Walk east", item.GetProperty("routeInstructionsFromPrevious")[0].GetString());
    }

    [Fact]
    public async Task Traveler_dashboard_preferences_round_trip_through_profile()
    {
        var traveler = await CreateUserAsync("Traveler", "dashboard-preferences");
        var update = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/traveler",
            traveler.Token,
            new
            {
                displayName = "Preference Traveler",
                preferredBudgetLevel = "Economy",
                preferredVibes = "Cultural, Adventure",
                comfortLevel = "Mid-range",
                safetyPriority = "Very High",
                interestIds = Array.Empty<Guid>()
            });
        update.EnsureSuccessStatusCode();
        var profile = await SendAsync(HttpMethod.Get, "/api/profiles/me", traveler.Token);
        profile.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(profile);
        Assert.Equal("Cultural, Adventure", json.RootElement.GetProperty("preferredVibes").GetString());
        Assert.Equal("Mid-range", json.RootElement.GetProperty("comfortLevel").GetString());
        Assert.Equal("Very High", json.RootElement.GetProperty("safetyPriority").GetString());
    }
}
