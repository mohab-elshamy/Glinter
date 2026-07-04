using Glinter.IntegrationTests.Infrastructure;
using System.Text.Json;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class IdentityProfilesRegionsSafetyCriticalTests : ApiTestBase
{
    public IdentityProfilesRegionsSafetyCriticalTests(GlinterApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Registration_rejects_an_email_that_differs_only_by_case()
    {
        // Arrange
        var email = $"normalized.{Guid.NewGuid():N}@glinter.test";
        var registration = new
        {
            fullName = "Normalized Email",
            email,
            password = GlinterApiFactory.UserPassword,
            role = "Traveler"
        };

        // Act
        var first = await Client.PostAsJsonAsync("/api/auth/register", registration);
        var duplicate = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            registration.fullName,
            email = email.ToUpperInvariant(),
            registration.password,
            registration.role
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await AssertProblemAsync(duplicate, 409, "conflict");
        Assert.Equal(
            1,
            await Factory.ScalarAsync<long>(
                $"""SELECT count(*) FROM users WHERE "NormalizedEmail" = '{email.ToUpperInvariant()}'"""));
    }

    [Fact]
    public async Task Invalid_login_does_not_reveal_whether_the_account_exists()
    {
        // Arrange
        var user = await CreateUserAsync("Traveler", "credential-privacy");

        // Act
        var knownAccount = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = user.Email,
            password = "DefinitelyWrong!2026"
        });
        var unknownAccount = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = $"missing.{Guid.NewGuid():N}@glinter.test",
            password = "DefinitelyWrong!2026"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, knownAccount.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownAccount.StatusCode);
        Assert.Equal(
            "application/problem+json",
            knownAccount.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            "application/problem+json",
            unknownAccount.Content.Headers.ContentType?.MediaType);
        var knownContent = await knownAccount.Content.ReadAsStringAsync();
        var unknownContent = await unknownAccount.Content.ReadAsStringAsync();
        using var knownJson = JsonDocument.Parse(knownContent);
        using var unknownJson = JsonDocument.Parse(unknownContent);
        Assert.Equal(
            "authentication_required",
            knownJson.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(
            "authentication_required",
            unknownJson.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(
            knownJson.RootElement.GetProperty("detail").GetString(),
            unknownJson.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain(user.Email, knownContent);
    }

    [Fact]
    public async Task Local_buddy_languages_and_interests_round_trip_after_normalization()
    {
        // Arrange
        var buddy = await CreateUserAsync("LocalBuddy", "profile-roundtrip");
        var interests = await Client.GetAsync("/api/interests");
        interests.EnsureSuccessStatusCode();
        using var interestsJson = await ReadJsonAsync(interests);
        var interestId = interestsJson.RootElement[0].GetProperty("id").GetGuid();

        // Act
        var update = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/local-buddy",
            buddy.Token,
            new
            {
                displayName = "Normalized Buddy",
                city = "Cairo",
                languages = " Arabic, English, arabic,  ",
                interestIds = new[] { interestId, interestId }
            });
        var profile = await SendAsync(
            HttpMethod.Get,
            "/api/profiles/me",
            buddy.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var profileJson = await ReadJsonAsync(profile);
        Assert.Equal("Arabic, English", profileJson.RootElement.GetProperty("languages").GetString());
        var savedInterest = Assert.Single(profileJson.RootElement.GetProperty("interests").EnumerateArray());
        Assert.Equal(interestId, savedInterest.GetProperty("id").GetGuid());
        Assert.Equal(
            1,
            await Factory.ScalarAsync<long>(
                $"""
                 SELECT count(*)
                 FROM buddy_interests bi
                 JOIN local_buddy_profiles p
                   ON p."Id" = bi."LocalBuddyProfileId"
                 WHERE bi."InterestId" = '{interestId}'
                   AND p."UserId" = '{buddy.UserId}'
                 """));
    }

    [Fact]
    public async Task Region_hierarchy_endpoints_return_only_the_expected_children()
    {
        // Arrange
        var hierarchy = await CreateHierarchyAsync();

        // Act
        var governorates = await Client.GetAsync(
            $"/api/regions/countries/{hierarchy.Adm0Gid}/governorates?pageSize=100");
        var districts = await Client.GetAsync(
            $"/api/regions/governorates/{hierarchy.Adm1Gid}/districts?pageSize=100");
        var neighbourhoods = await Client.GetAsync(
            $"/api/regions/districts/{hierarchy.Adm2Gid}/neighbourhoods?pageSize=100");

        // Assert
        governorates.EnsureSuccessStatusCode();
        districts.EnsureSuccessStatusCode();
        neighbourhoods.EnsureSuccessStatusCode();
        using var governoratesJson = await ReadJsonAsync(governorates);
        using var districtsJson = await ReadJsonAsync(districts);
        using var neighbourhoodsJson = await ReadJsonAsync(neighbourhoods);
        Assert.Contains(
            governoratesJson.RootElement.EnumerateArray(),
            item => item.GetProperty("gid").GetInt32() == hierarchy.Adm1Gid);
        Assert.Contains(
            districtsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("gid").GetInt32() == hierarchy.Adm2Gid);
        Assert.Contains(
            neighbourhoodsJson.RootElement.EnumerateArray(),
            item => item.GetProperty("gid").GetInt32() == hierarchy.Adm3Gid);
    }

    [Fact]
    public async Task Point_lookup_returns_the_matching_complete_region_hierarchy()
    {
        // Arrange
        var hierarchy = await CreateHierarchyAsync();
        const string polygon =
            "ST_Multi(ST_GeomFromText('POLYGON((30 29,32 29,32 31,30 31,30 29))', 4326))";
        await Factory.ExecuteAsync(
            $"""
             UPDATE adm0 SET boundary_geom = {polygon} WHERE gid = {hierarchy.Adm0Gid};
             UPDATE adm1 SET boundary_geom = {polygon} WHERE gid = {hierarchy.Adm1Gid};
             UPDATE adm2 SET boundary_geom = {polygon} WHERE gid = {hierarchy.Adm2Gid};
             UPDATE adm3 SET boundary_geom = {polygon} WHERE gid = {hierarchy.Adm3Gid};
             """);

        // Act
        var response = await Client.GetAsync("/api/regions/by-point?lat=30&lon=31");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(hierarchy.Adm0Gid, json.RootElement.GetProperty("adm0Gid").GetInt32());
        Assert.Equal(hierarchy.Adm1Gid, json.RootElement.GetProperty("adm1Gid").GetInt32());
        Assert.Equal(hierarchy.Adm2Gid, json.RootElement.GetProperty("adm2Gid").GetInt32());
        Assert.Equal(hierarchy.Adm3Gid, json.RootElement.GetProperty("adm3Gid").GetInt32());
    }

    [Fact]
    public async Task Stored_safety_scores_are_returned_with_region_identity()
    {
        // Arrange
        var hierarchy = await CreateHierarchyAsync();
        await Factory.ExecuteAsync(
            $"""
             INSERT INTO safety_index_results
                 ("Adm2Gid", "WeeklyScore", "WeeklyGeneralSafetyDescription",
                  "WeeklyTrendingEventDescription", "WeeklyCalculatedAtUtc",
                  "WeeklyNewsItemCount", "HistoricalScore",
                  "HistoricalGeneralSafetyDescription",
                  "HistoricalTrendingEventDescription",
                  "HistoricalCalculatedAtUtc", "HistoricalNewsItemCount",
                  "CreatedAtUtc", "UpdatedAtUtc")
             VALUES
                 ({hierarchy.Adm2Gid}, 82, 'Weekly stored result',
                  'No current trend', NOW(), 4, 71, 'Historical stored result',
                  'Stable history', NOW(), 12, NOW(), NOW())
             """);

        // Act
        var response = await Client.GetAsync(
            $"/api/safety-index/adm2/{hierarchy.Adm2Gid}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(hierarchy.Adm2Gid, json.RootElement.GetProperty("adm2Gid").GetInt32());
        Assert.Equal(82, json.RootElement.GetProperty("weeklyScore").GetProperty("score").GetInt32());
        Assert.Equal(4, json.RootElement.GetProperty("weeklyScore").GetProperty("newsItemCount").GetInt32());
        Assert.Equal(71, json.RootElement.GetProperty("historicalScore").GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task Unknown_safety_region_returns_not_found_without_external_work()
    {
        // Arrange
        var missingAdm2Gid = int.MaxValue;

        // Act
        var response = await Client.GetAsync(
            $"/api/safety-index/adm2/{missingAdm2Gid}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var json = await ReadJsonAsync(response);
        Assert.Equal(
            "ADM2 area not found.",
            json.RootElement.GetProperty("message").GetString());
    }

    private async Task<RegionHierarchy> CreateHierarchyAsync()
    {
        var adminToken = await GetAdminTokenAsync();
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var country = await SendAsync(
            HttpMethod.Post,
            "/api/regions/countries",
            adminToken,
            new { nameEn = $"Country {suffix}", pcode = $"C{suffix}" });
        Assert.Equal(HttpStatusCode.Created, country.StatusCode);
        using var countryJson = await ReadJsonAsync(country);
        var adm0Gid = countryJson.RootElement.GetProperty("gid").GetInt32();

        var governorate = await SendAsync(
            HttpMethod.Post,
            "/api/regions/governorates",
            adminToken,
            new { adm0Gid, nameEn = $"Governorate {suffix}", pcode = $"G{suffix}" });
        Assert.Equal(HttpStatusCode.Created, governorate.StatusCode);
        using var governorateJson = await ReadJsonAsync(governorate);
        var adm1Gid = governorateJson.RootElement.GetProperty("gid").GetInt32();

        var district = await SendAsync(
            HttpMethod.Post,
            "/api/regions/districts",
            adminToken,
            new { adm1Gid, nameEn = $"District {suffix}", pcode = $"D{suffix}" });
        Assert.Equal(HttpStatusCode.Created, district.StatusCode);
        using var districtJson = await ReadJsonAsync(district);
        var adm2Gid = districtJson.RootElement.GetProperty("gid").GetInt32();

        var neighbourhood = await SendAsync(
            HttpMethod.Post,
            "/api/regions/neighbourhoods",
            adminToken,
            new { adm2Gid, nameEn = $"Neighbourhood {suffix}", pcode = $"N{suffix}" });
        Assert.Equal(HttpStatusCode.Created, neighbourhood.StatusCode);
        using var neighbourhoodJson = await ReadJsonAsync(neighbourhood);

        return new RegionHierarchy(
            adm0Gid,
            adm1Gid,
            adm2Gid,
            neighbourhoodJson.RootElement.GetProperty("gid").GetInt32());
    }

    private sealed record RegionHierarchy(
        int Adm0Gid,
        int Adm1Gid,
        int Adm2Gid,
        int Adm3Gid);
}
