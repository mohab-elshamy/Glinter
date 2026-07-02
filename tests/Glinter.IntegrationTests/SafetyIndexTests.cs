using System.Net;
using System.Net.Http.Json;
using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.SafetyIndex.Application.Dtos;
using Glinter.Modules.SafetyIndex.Domain.Entities;
using Glinter.Modules.SafetyIndex.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class SafetyIndexTests : ApiTestBase
{
    public SafetyIndexTests(GlinterApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Adm2_returns_empty_scores_when_no_result_exists()
    {
        var adminToken = await GetAdminTokenAsync();
        var hierarchy = await CreateSafetyHierarchyAsync(
            adminToken,
            $"District {Guid.NewGuid():N}");

        var response = await Client.GetAsync(
            $"/api/safety-index/adm2/{hierarchy.Adm2Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<Adm2SafetyIndexResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(hierarchy.Adm2Id, result.Adm2Gid);
        Assert.Equal(hierarchy.Adm1Id, result.Adm1Gid);

        Assert.Null(result.WeeklyScore.Score);
        Assert.Null(result.WeeklyScore.CalculatedAtUtc);
        Assert.Equal(0, result.WeeklyScore.NewsItemCount);

        Assert.Null(result.HistoricalScore.Score);
        Assert.Null(result.HistoricalScore.CalculatedAtUtc);
        Assert.Equal(0, result.HistoricalScore.NewsItemCount);
    }

    [Fact]
    public async Task Adm2_returns_persisted_weekly_and_historical_scores()
    {
        var adminToken = await GetAdminTokenAsync();
        var hierarchy = await CreateSafetyHierarchyAsync(
            adminToken,
            $"Scored District {Guid.NewGuid():N}");

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<SafetyIndexDbContext>();

            db.SafetyIndexResults.Add(new SafetyIndexResult
            {
                Adm2Gid = hierarchy.Adm2Id,

                WeeklyScore = 82,
                WeeklyGeneralSafetyDescription =
                    "The district is generally safe this week.",
                WeeklyTrendingEventDescription =
                    "No major safety incidents were detected.",
                WeeklyCalculatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
                WeeklyNewsItemCount = 6,

                HistoricalScore = 74,
                HistoricalGeneralSafetyDescription =
                    "The district has a stable historical safety record.",
                HistoricalTrendingEventDescription =
                    "Historical reports show occasional minor incidents.",
                HistoricalCalculatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
                HistoricalNewsItemCount = 18
            });

            await db.SaveChangesAsync();
        }

        var response = await Client.GetAsync(
            $"/api/safety-index/adm2/{hierarchy.Adm2Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result =
            await response.Content.ReadFromJsonAsync<Adm2SafetyIndexResponseDto>();

        Assert.NotNull(result);

        Assert.Equal(82, result.WeeklyScore.Score);
        Assert.Equal(6, result.WeeklyScore.NewsItemCount);
        Assert.NotNull(result.WeeklyScore.CalculatedAtUtc);
        Assert.Equal(
            "The district is generally safe this week.",
            result.WeeklyScore.GeneralSafetyDescription);

        Assert.Equal(74, result.HistoricalScore.Score);
        Assert.Equal(18, result.HistoricalScore.NewsItemCount);
        Assert.NotNull(result.HistoricalScore.CalculatedAtUtc);
        Assert.Equal(
            "The district has a stable historical safety record.",
            result.HistoricalScore.GeneralSafetyDescription);
    }

    [Fact]
    public async Task Adm1_and_adm0_return_their_child_adm2_areas_sorted_by_name()
    {
        var adminToken = await GetAdminTokenAsync();
        var marker = Guid.NewGuid().ToString("N")[..10];

        var zuluName = $"Zulu District {marker}";
        var alphaName = $"Alpha District {marker}";

        var hierarchy = await CreateSafetyHierarchyAsync(
            adminToken,
            zuluName);

        var alphaAdm2Id = await CreateAdm2Async(
            adminToken,
            hierarchy.Adm1Id,
            alphaName);

        var adm1Response = await Client.GetAsync(
            $"/api/safety-index/adm1/{hierarchy.Adm1Id}");

        Assert.Equal(HttpStatusCode.OK, adm1Response.StatusCode);

        var adm1Results =
            await adm1Response.Content
                .ReadFromJsonAsync<List<Adm2SafetyIndexResponseDto>>();

        Assert.NotNull(adm1Results);
        Assert.Equal(2, adm1Results.Count);
        Assert.Equal(
            new[] { alphaName, zuluName },
            adm1Results.Select(x => x.NameEn).ToArray());

        Assert.Contains(adm1Results, x => x.Adm2Gid == hierarchy.Adm2Id);
        Assert.Contains(adm1Results, x => x.Adm2Gid == alphaAdm2Id);

        var adm0Response = await Client.GetAsync(
            $"/api/safety-index/adm0/{hierarchy.Adm0Id}");

        Assert.Equal(HttpStatusCode.OK, adm0Response.StatusCode);

        var adm0Results =
            await adm0Response.Content
                .ReadFromJsonAsync<List<Adm2SafetyIndexResponseDto>>();

        Assert.NotNull(adm0Results);
        Assert.Equal(2, adm0Results.Count);
        Assert.Contains(adm0Results, x => x.Adm2Gid == hierarchy.Adm2Id);
        Assert.Contains(adm0Results, x => x.Adm2Gid == alphaAdm2Id);
    }

    [Fact]
    public async Task Unknown_region_ids_return_the_current_documented_responses()
    {
        const int unknownId = int.MaxValue;

        var adm2Response = await Client.GetAsync(
            $"/api/safety-index/adm2/{unknownId}");

        Assert.Equal(HttpStatusCode.NotFound, adm2Response.StatusCode);

        using (var json = await ReadJsonAsync(adm2Response))
        {
            Assert.Equal(
                "ADM2 area not found.",
                json.RootElement.GetProperty("message").GetString());
        }

        var adm1Response = await Client.GetAsync(
            $"/api/safety-index/adm1/{unknownId}");

        Assert.Equal(HttpStatusCode.OK, adm1Response.StatusCode);

        var adm1Results =
            await adm1Response.Content
                .ReadFromJsonAsync<List<Adm2SafetyIndexResponseDto>>();

        Assert.NotNull(adm1Results);
        Assert.Empty(adm1Results);

        var adm0Response = await Client.GetAsync(
            $"/api/safety-index/adm0/{unknownId}");

        Assert.Equal(HttpStatusCode.OK, adm0Response.StatusCode);

        var adm0Results =
            await adm0Response.Content
                .ReadFromJsonAsync<List<Adm2SafetyIndexResponseDto>>();

        Assert.NotNull(adm0Results);
        Assert.Empty(adm0Results);
    }

    private async Task<RegionHierarchy> CreateSafetyHierarchyAsync(
        string adminToken,
        string adm2Name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var countryResponse = await SendAsync(
            HttpMethod.Post,
            "/api/regions/countries",
            adminToken,
            new
            {
                nameEn = $"Country {suffix}",
                pcode = $"C{suffix}"
            });

        countryResponse.EnsureSuccessStatusCode();

        using var countryJson = await ReadJsonAsync(countryResponse);
        var adm0Id = countryJson.RootElement
            .GetProperty("gid")
            .GetInt32();

        var governorateResponse = await SendAsync(
            HttpMethod.Post,
            "/api/regions/governorates",
            adminToken,
            new
            {
                adm0Gid = adm0Id,
                nameEn = $"Governorate {suffix}",
                pcode = $"G{suffix}"
            });

        governorateResponse.EnsureSuccessStatusCode();

        using var governorateJson = await ReadJsonAsync(governorateResponse);
        var adm1Id = governorateJson.RootElement
            .GetProperty("gid")
            .GetInt32();

        var adm2Id = await CreateAdm2Async(
            adminToken,
            adm1Id,
            adm2Name);

        return new RegionHierarchy(adm0Id, adm1Id, adm2Id);
    }

    private async Task<int> CreateAdm2Async(
        string adminToken,
        int adm1Id,
        string name)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/regions/districts",
            adminToken,
            new
            {
                adm1Gid = adm1Id,
                nameEn = name,
                pcode = $"D{suffix}"
            });

        response.EnsureSuccessStatusCode();

        using var json = await ReadJsonAsync(response);

        return json.RootElement
            .GetProperty("gid")
            .GetInt32();
    }

    private sealed record RegionHierarchy(
        int Adm0Id,
        int Adm1Id,
        int Adm2Id);
}