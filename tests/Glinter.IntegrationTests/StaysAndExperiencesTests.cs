using System.Net;
using System.Text.Json;
using Glinter.IntegrationTests.Infrastructure;
using System.Net.Http.Headers;
using System.Text;


namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class StaysAndExperiencesTests : ApiTestBase
{
    public StaysAndExperiencesTests(GlinterApiFactory factory) : base(factory)
    {
    }
    
    
[Fact]
public async Task Anonymous_user_cannot_import_stays()
{
    var response = await SendImportAsync(
        "/api/stays/import",
        token: null,
        json: """
              [
                {
                  "name": "Anonymous Imported Stay"
                }
              ]
              """);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task Traveler_cannot_import_stays()
{
    var traveler = await CreateUserAsync(
        "Traveler",
        "traveler-import-stay");

    var response = await SendImportAsync(
        "/api/stays/import",
        traveler.Token,
        """
        [
          {
            "name": "Traveler Imported Stay"
          }
        ]
        """);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task Stay_import_requires_a_file()
{
    var adminToken = await GetAdminTokenAsync();

    var response = await SendImportAsync(
        "/api/stays/import",
        adminToken,
        json: string.Empty);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "JSON file is required.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Stay_import_rejects_invalid_json()
{
    var adminToken = await GetAdminTokenAsync();

    var response = await SendImportAsync(
        "/api/stays/import",
        adminToken,
        "{ this is not valid JSON");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Uploaded file must contain valid JSON.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Stay_import_rejects_an_empty_array()
{
    var adminToken = await GetAdminTokenAsync();

    var response = await SendImportAsync(
        "/api/stays/import",
        adminToken,
        "[]");

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Import file must contain at least one stay.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Experience_import_requires_a_category()
{
    var adminToken = await GetAdminTokenAsync();

    var response = await SendImportAsync(
        "/api/experiences/import",
        adminToken,
        """
        [
          {
            "name": "Imported Experience Without Category"
          }
        ]
        """,
        category: null);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Category is required.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Admin_can_import_a_valid_stay_file()
{
    var adminToken = await GetAdminTokenAsync();
    var name = $"Imported Stay {Guid.NewGuid():N}";

    var importJson =
        $$"""
          [
            {
              "name": "{{name}}",
              "price": "900",
              "description": "Imported integration-test stay",
              "coordinates": {
                "latitude": 30.05,
                "longitude": 31.25
              }
            }
          ]
          """;

    var response = await SendImportAsync(
        "/api/stays/import",
        adminToken,
        importJson);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using (var resultJson = await ReadJsonAsync(response))
    {
        Assert.Equal(
            1,
            resultJson.RootElement.GetProperty("created").GetInt32());

        Assert.Equal(
            0,
            resultJson.RootElement.GetProperty("skipped").GetInt32());
    }

    var searchResponse = await Client.GetAsync(
        $"/api/stays?search={Uri.EscapeDataString(name)}&page=1&pageSize=10");

    Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

    using var searchJson = await ReadJsonAsync(searchResponse);

    Assert.Contains(
        searchJson.RootElement.GetProperty("items").EnumerateArray(),
        item => item.GetProperty("name").GetString() == name);
}

[Fact]
public async Task Admin_can_import_a_valid_experience_file()
{
    var adminToken = await GetAdminTokenAsync();
    var name = $"Imported Experience {Guid.NewGuid():N}";

    var importJson =
        $$"""
          [
            {
              "name": "{{name}}",
              "description": "Imported integration-test experience",
              "address": "Integration test address",
              "coordinates": {
                "latitude": 30.05,
                "longitude": 31.25
              }
            }
          ]
          """;

    var response = await SendImportAsync(
        "/api/experiences/import",
        adminToken,
        importJson,
        category: "Historical");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using (var resultJson = await ReadJsonAsync(response))
    {
        Assert.Equal(
            1,
            resultJson.RootElement.GetProperty("created").GetInt32());

        Assert.Equal(
            0,
            resultJson.RootElement.GetProperty("skipped").GetInt32());
    }

    var searchResponse = await Client.GetAsync(
        $"/api/experiences?search={Uri.EscapeDataString(name)}" +
        "&category=Historical&page=1&pageSize=10");

    Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

    using var searchJson = await ReadJsonAsync(searchResponse);

    Assert.Contains(
        searchJson.RootElement.GetProperty("items").EnumerateArray(),
        item => item.GetProperty("name").GetString() == name);
}
    
[Fact]
public async Task Anonymous_user_cannot_create_a_stay()
{
    var response = await SendAsync(
        HttpMethod.Post,
        "/api/stays",
        body: new
        {
            name = "Anonymous Stay",
            price = 500
        });

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task Traveler_cannot_create_a_stay()
{
    var traveler = await CreateUserAsync(
        "Traveler",
        "traveler-create-stay");

    var response = await SendAsync(
        HttpMethod.Post,
        "/api/stays",
        traveler.Token,
        new
        {
            name = "Unauthorized Traveler Stay",
            price = 500
        });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task Stay_name_is_required()
{
    var owner = await CreateUserAsync(
        "HotelOwner",
        "invalid-stay-owner");

    await UpsertHotelOwnerAsync(owner);

    var response = await SendAsync(
        HttpMethod.Post,
        "/api/stays",
        owner.Token,
        new
        {
            name = "   ",
            price = 500
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Stay name is required.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Anonymous_user_cannot_create_an_experience()
{
    var response = await SendAsync(
        HttpMethod.Post,
        "/api/experiences",
        body: new
        {
            category = "Historical",
            name = "Anonymous Experience"
        });

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task Traveler_cannot_create_an_experience()
{
    var traveler = await CreateUserAsync(
        "Traveler",
        "traveler-create-experience");

    var response = await SendAsync(
        HttpMethod.Post,
        "/api/experiences",
        traveler.Token,
        new
        {
            category = "Historical",
            name = "Unauthorized Traveler Experience"
        });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task Experience_name_is_required()
{
    var provider = await CreateUserAsync(
        "ExperienceProvider",
        "invalid-experience-provider");

    await UpsertProviderAsync(provider);

    var response = await SendAsync(
        HttpMethod.Post,
        "/api/experiences",
        provider.Token,
        new
        {
            category = "Historical",
            name = "   "
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Experience name is required.",
        json.RootElement.GetProperty("message").GetString());
}    
    [Fact]
    public async Task Pending_created_experience_does_not_appear_in_map_results()
{
    var experience = await CreateExperienceWithVisitDataAsync();

    var response = await Client.GetAsync(
        $"/api/experiences/map?search={Uri.EscapeDataString(experience.Name)}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.DoesNotContain(
        json.RootElement.EnumerateArray(),
        item =>
            item.GetProperty("id").GetInt32() == experience.Id &&
            item.GetProperty("name").GetString() == experience.Name);
}

[Fact]
public async Task Visit_insight_returns_open_and_busy_for_known_time()
{
    var experience = await CreateExperienceWithVisitDataAsync();

    var visitAt = Uri.EscapeDataString("2026-07-06T10:30:00");

    var response = await Client.GetAsync(
        $"/api/experiences/{experience.Id}/visit-insights?visitAt={visitAt}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var json = await ReadJsonAsync(response);
    var root = json.RootElement;

    Assert.True(root.GetProperty("isOpen").GetBoolean());
    Assert.Equal("open", root.GetProperty("openStatus").GetString());
    Assert.Equal(60, root.GetProperty("popularityPercentage").GetInt32());
    Assert.Equal("busy", root.GetProperty("crowdLevel").GetString());
    Assert.Equal(
        "09:00-17:00",
        root.GetProperty("bestKnownOpenWindow").GetString());
}

[Fact]
public async Task Visit_insight_returns_closed_outside_opening_hours()
{
    var experience = await CreateExperienceWithVisitDataAsync();

    var visitAt = Uri.EscapeDataString("2026-07-06T18:30:00");

    var response = await Client.GetAsync(
        $"/api/experiences/{experience.Id}/visit-insights?visitAt={visitAt}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var json = await ReadJsonAsync(response);
    var root = json.RootElement;

    Assert.False(root.GetProperty("isOpen").GetBoolean());
    Assert.Equal("closed", root.GetProperty("openStatus").GetString());
    Assert.Equal("unknown", root.GetProperty("crowdLevel").GetString());
    Assert.Equal(
        JsonValueKind.Null,
        root.GetProperty("bestKnownOpenWindow").ValueKind);
}

[Fact]
public async Task Visit_insight_returns_unknown_when_no_hours_exist_for_day()
{
    var experience = await CreateExperienceWithVisitDataAsync();

    // 7 July 2026 is Tuesday; the test experience only has Monday hours.
    var visitAt = Uri.EscapeDataString("2026-07-07T10:30:00");

    var response = await Client.GetAsync(
        $"/api/experiences/{experience.Id}/visit-insights?visitAt={visitAt}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    using var json = await ReadJsonAsync(response);
    var root = json.RootElement;

    Assert.Equal(JsonValueKind.Null, root.GetProperty("isOpen").ValueKind);
    Assert.Equal("unknown", root.GetProperty("openStatus").GetString());
    Assert.Equal("unknown", root.GetProperty("crowdLevel").GetString());
}

[Fact]

public async Task Unknown_experience_read_endpoints_return_not_found()
{
    const int unknownId = int.MaxValue;
    var adminToken = await GetAdminTokenAsync();

    var paths = new[]
    {
        $"/api/experiences/{unknownId}",
        $"/api/experiences/{unknownId}/reviews",
        $"/api/experiences/{unknownId}/reviews/llm-input",
        $"/api/experiences/{unknownId}/visit-insights?visitAt=2026-07-06T10:30:00"
    };

    foreach (var path in paths)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                adminToken);

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

private async Task<TestExperience> CreateExperienceWithVisitDataAsync()
{
    var provider = await CreateUserAsync(
        "ExperienceProvider",
        "visit-insight-provider");

    await UpsertProviderAsync(provider);

    var name = $"Visit Insight Experience {Guid.NewGuid():N}";

    var response = await SendAsync(
        HttpMethod.Post,
        "/api/experiences",
        provider.Token,
        new
        {
            category = "Historical",
            name,
            description = "Experience created for visit-insight testing.",
            address = "Integration test address",
            latitude = 30.05,
            longitude = 31.25,
            featuredImageLinks = new[]
            {
                "https://example.test/visit-insight.jpg"
            },
            hours = new[]
            {
                new
                {
                    dayOfWeek = DayOfWeek.Monday,
                    opensAt = "09:00:00",
                    closesAt = "17:00:00"
                }
            },
            popularTimes = new[]
            {
                new
                {
                    dayOfWeek = DayOfWeek.Monday,
                    hourOfDay = 10,
                    popularityPercentage = 60
                }
            },
            amenities = new[]
            {
                "Guided tour"
            }
        });

    response.EnsureSuccessStatusCode();

    using var json = await ReadJsonAsync(response);

    return new TestExperience(
        json.RootElement.GetProperty("id").GetInt32(),
        name);
}

private sealed record TestExperience(int Id, string Name);
    

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
    public async Task Experience_provider_can_create_a_pending_experience_that_is_not_publicly_visible()
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

        var details = await Client.GetAsync(
            $"/api/experiences/{experienceId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            details.StatusCode);

        var search = await Client.GetAsync(
            $"/api/experiences?search={Uri.EscapeDataString(name)}" +
            "&category=Historical&page=1&pageSize=10");

        Assert.Equal(
            HttpStatusCode.OK,
            search.StatusCode);

        using var searchJson = await ReadJsonAsync(search);

        Assert.DoesNotContain(
            searchJson.RootElement
                .GetProperty("items")
                .EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == experienceId);
    }
    [Fact]
public async Task Traveler_can_create_a_stay_review_and_read_it_publicly()
{
    var stayId = await CreateStayForReviewAsync();

    var traveler = await CreateUserAsync("Traveler", "stay-reviewer");
    var reviewText = $"Excellent integration stay {Guid.NewGuid():N}";

    var createReview = await SendAsync(
        HttpMethod.Post,
        $"/api/stays/{stayId}/reviews",
        traveler.Token,
        new
        {
            rating = 5,
            reviewText
        });

    Assert.Equal(HttpStatusCode.OK, createReview.StatusCode);

    using var createdJson = await ReadJsonAsync(createReview);

    Assert.Equal(5, createdJson.RootElement.GetProperty("rating").GetInt32());
    Assert.Equal(
        reviewText,
        createdJson.RootElement.GetProperty("reviewText").GetString());
    Assert.Equal(
        "glinter",
        createdJson.RootElement.GetProperty("platform").GetString());

    var reviewsResponse = await Client.GetAsync(
        $"/api/stays/{stayId}/reviews?page=1&pageSize=20");

    Assert.Equal(HttpStatusCode.OK, reviewsResponse.StatusCode);

    using var reviewsJson = await ReadJsonAsync(reviewsResponse);

    Assert.Contains(
        reviewsJson.RootElement.EnumerateArray(),
        review =>
            review.GetProperty("reviewText").GetString() == reviewText &&
            review.GetProperty("rating").GetInt32() == 5);
}

[Theory]
[InlineData(0)]
[InlineData(6)]
public async Task Stay_review_rating_outside_one_to_five_is_rejected(int rating)
{
    var stayId = await CreateStayForReviewAsync();
    var traveler = await CreateUserAsync("Traveler", "invalid-rating");

    var response = await SendAsync(
        HttpMethod.Post,
        $"/api/stays/{stayId}/reviews",
        traveler.Token,
        new
        {
            rating,
            reviewText = "This review has an invalid rating."
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Rating must be between 1 and 5.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Empty_stay_review_text_is_rejected()
{
    var stayId = await CreateStayForReviewAsync();
    var traveler = await CreateUserAsync("Traveler", "empty-review");

    var response = await SendAsync(
        HttpMethod.Post,
        $"/api/stays/{stayId}/reviews",
        traveler.Token,
        new
        {
            rating = 4,
            reviewText = ""
        });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Review text is required.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Reviewing_an_unknown_stay_returns_not_found()
{
    var traveler = await CreateUserAsync("Traveler", "unknown-stay-review");

    var response = await SendAsync(
        HttpMethod.Post,
        $"/api/stays/{int.MaxValue}/reviews",
        traveler.Token,
        new
        {
            rating = 5,
            reviewText = "This stay does not exist."
        });

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    using var json = await ReadJsonAsync(response);

    Assert.Equal(
        "Stay not found.",
        json.RootElement.GetProperty("message").GetString());
}

[Fact]
public async Task Anonymous_user_cannot_create_a_stay_review()
{
    var stayId = await CreateStayForReviewAsync();

    var response = await SendAsync(
        HttpMethod.Post,
        $"/api/stays/{stayId}/reviews",
        body: new
        {
            rating = 5,
            reviewText = "Anonymous review."
        });

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}

[Fact]
public async Task Hotel_owner_cannot_create_a_traveler_stay_review()
{
    var stayId = await CreateStayForReviewAsync();

    var hotelOwner = await CreateUserAsync(
        "HotelOwner",
        "unauthorized-review-owner");

    await UpsertHotelOwnerAsync(hotelOwner);

    var response = await SendAsync(
        HttpMethod.Post,
        $"/api/stays/{stayId}/reviews",
        hotelOwner.Token,
        new
        {
            rating = 5,
            reviewText = "Hotel owners cannot use the traveler review endpoint."
        });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

private async Task<int> CreateStayForReviewAsync()
{
    var owner = await CreateUserAsync(
        "HotelOwner",
        "review-stay-owner");

    await UpsertHotelOwnerAsync(owner);

    var response = await SendAsync(
        HttpMethod.Post,
        "/api/stays",
        owner.Token,
        new
        {
            name = $"Review Test Stay {Guid.NewGuid():N}",
            price = 800,
            description = "Stay created for integration review testing.",
            latitude = 30.05,
            longitude = 31.25,
            imageLinks = new[]
            {
                "https://example.test/review-stay.jpg"
            },
            amenities = new[]
            {
                "Wi-Fi"
            },
            bookingPlatforms = Array.Empty<object>()
        });

    response.EnsureSuccessStatusCode();

    using var json = await ReadJsonAsync(response);

    return json.RootElement.GetProperty("id").GetInt32();
}

private async Task<HttpResponseMessage> SendImportAsync(
    string path,
    string? token,
    string? json,
    string? category = null)
{
    using var form = new MultipartFormDataContent();

    if (!string.IsNullOrWhiteSpace(category))
    {
        form.Add(
            new StringContent(category),
            "Category");
    }

    if (json is not null)
    {
        var fileContent = new ByteArrayContent(
            Encoding.UTF8.GetBytes(json));

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("application/json");

        form.Add(
            fileContent,
            "File",
            "integration-import.json");
    }

    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        path)
    {
        Content = form
    };

    if (!string.IsNullOrWhiteSpace(token))
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);
    }

    return await Client.SendAsync(request);
}

}

