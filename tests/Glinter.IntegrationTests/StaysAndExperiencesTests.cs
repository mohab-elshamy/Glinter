using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class StaysAndExperiencesTests : ApiTestBase
{
    public StaysAndExperiencesTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Hotel_owner_can_create_and_publicly_search_a_stay()
    {
        var owner = await CreateUserAsync("HotelOwner", "stay-owner");
        await UpsertHotelOwnerAsync(owner);

        var name = $"Integration Stay {Guid.NewGuid():N}";
        var create = await SendAsync(
            HttpMethod.Post,
            "/api/stays",
            owner.Token,
            new
            {
                name,
                price = 1000,
                description = "Integration stay",
                latitude = 30.05,
                longitude = 31.25,
                imageLinks = new[] { "https://example.test/stay.jpg" },
                amenities = new[] { "Wi-Fi" },
                bookingPlatforms = new[]
                {
                    new
                    {
                        name = "Glinter",
                        priceWithTax = 1150,
                        link = "https://example.test/book"
                    }
                }
            });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdJson = await ReadJsonAsync(create);
        var stayId = createdJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(owner.UserId, createdJson.RootElement.GetProperty("createdByUserId").GetGuid());
        Assert.NotEqual(
            Guid.Empty,
            createdJson.RootElement.GetProperty("hotelOwnerProfileId").GetGuid());

        var details = await Client.GetAsync($"/api/stays/{stayId}");
        details.EnsureSuccessStatusCode();

        var search = await Client.GetAsync(
            $"/api/stays?search={Uri.EscapeDataString(name)}&page=1&pageSize=10");
        search.EnsureSuccessStatusCode();
        using var searchJson = await ReadJsonAsync(search);
        Assert.Contains(
            searchJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == stayId);
    }

    [Fact]
    public async Task Experience_provider_can_create_and_publicly_search_an_experience()
    {
        var provider = await CreateUserAsync(
            "ExperienceProvider",
            "experience-provider");
        await UpsertProviderAsync(provider);

        var name = $"Integration Experience {Guid.NewGuid():N}";
        var create = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            provider.Token,
            new
            {
                category = "Historical",
                name,
                description = "Integration experience",
                address = "Integration neighbourhood",
                latitude = 30.05,
                longitude = 31.25,
                featuredImageLinks = new[] { "https://example.test/experience.jpg" },
                amenities = new[] { "Guided tour" }
            });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdJson = await ReadJsonAsync(create);
        var experienceId = createdJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(
            provider.UserId,
            createdJson.RootElement.GetProperty("createdByUserId").GetGuid());
        Assert.NotEqual(
            Guid.Empty,
            createdJson.RootElement.GetProperty("providerProfileId").GetGuid());

        var details = await Client.GetAsync($"/api/experiences/{experienceId}");
        details.EnsureSuccessStatusCode();

        var search = await Client.GetAsync(
            $"/api/experiences?search={Uri.EscapeDataString(name)}&category=Historical&page=1&pageSize=10");
        search.EnsureSuccessStatusCode();
        using var searchJson = await ReadJsonAsync(search);
        Assert.Contains(
            searchJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == experienceId);
    }

    [Fact]
    public async Task Structured_hotel_recommendations_rank_by_preferences_without_groq()
    {
        var owner = await CreateUserAsync("HotelOwner", "recommendation-owner");
        await UpsertHotelOwnerAsync(owner);

        var provider = await CreateUserAsync("ExperienceProvider", "recommendation-provider");
        await UpsertProviderAsync(provider);

        var suffix = Guid.NewGuid().ToString("N");
        var bestStayId = await CreateStayAsync(
            owner.Token,
            $"Recommendation Best Stay {suffix}",
            price: 1500,
            latitude: 30.0500,
            longitude: 31.2400,
            amenities: ["Wi-Fi", "Gym"]);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Far Stay {suffix}",
            price: 1500,
            latitude: 30.4500,
            longitude: 31.7000,
            amenities: ["Parking"]);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Luxury Stay {suffix}",
            price: 5000,
            latitude: 30.0520,
            longitude: 31.2420,
            amenities: ["Wi-Fi"]);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Budget Stay {suffix}",
            price: 500,
            latitude: 30.0550,
            longitude: 31.2450,
            amenities: ["Gym"]);

        await CreateStayAsync(
            owner.Token,
            $"Recommendation Economy Stay {suffix}",
            price: 1000,
            latitude: 30.0560,
            longitude: 31.2460,
            amenities: []);

        await CreateExperienceAsync(
            provider.Token,
            "Historical",
            $"Recommendation Museum {suffix}",
            latitude: 30.0505,
            longitude: 31.2405);

        await CreateExperienceAsync(
            provider.Token,
            "Dining",
            $"Recommendation Restaurant {suffix}",
            latitude: 30.0510,
            longitude: 31.2410);

        var response = await Client.PostAsJsonAsync("/api/stays/recommendations", new
        {
            budgetLevel = 3,
            experienceCategories = new[]
            {
                new { category = "Historical", weight = 50 },
                new { category = "Dining", weight = 50 }
            },
            requestedAmenities = new[] { "WiFi", "Gym" },
            limit = 5,
            preferredLanguage = "en"
        });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.NotEmpty(items);
        Assert.Equal(bestStayId, items[0].GetProperty("hotelId").GetInt32());
        Assert.Equal(1, items[0].GetProperty("ranking").GetInt32());
        Assert.Equal(3, items[0].GetProperty("budgetLevel").GetInt32());
        Assert.Contains(
            items[0].GetProperty("matchedAmenities").EnumerateArray(),
            amenity => amenity.GetString() == "WiFi");
        Assert.Contains(
            items[0].GetProperty("matchedAmenities").EnumerateArray(),
            amenity => amenity.GetString() == "Gym");
        Assert.False(items[0]
            .GetProperty("explanation")
            .GetProperty("isAiGenerated")
            .GetBoolean());
    }

    private async Task<int> CreateStayAsync(
        string token,
        string name,
        decimal price,
        double latitude,
        double longitude,
        string[] amenities)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/stays",
            token,
            new
            {
                name,
                price,
                description = "Recommendation integration stay",
                latitude,
                longitude,
                amenities
            });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<int> CreateExperienceAsync(
        string token,
        string category,
        string name,
        double latitude,
        double longitude)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            token,
            new
            {
                category,
                name,
                description = "Recommendation integration experience",
                address = "Recommendation integration neighbourhood",
                latitude,
                longitude
            });

        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("id").GetInt32();
    }
}
