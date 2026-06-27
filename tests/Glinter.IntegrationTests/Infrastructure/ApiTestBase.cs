using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit.Sdk;

namespace Glinter.IntegrationTests.Infrastructure;

public abstract class ApiTestBase
{
    protected ApiTestBase(GlinterApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected GlinterApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected async Task<TestUser> CreateUserAsync(string role, string? prefix = null)
    {
        var email = $"{prefix ?? role.ToLowerInvariant()}.{Guid.NewGuid():N}@glinter.test";
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"Integration {role}",
            email,
            password = GlinterApiFactory.UserPassword,
            role
        });
        response.EnsureSuccessStatusCode();

        var json = await ReadJsonAsync(response);
        return new TestUser(
            json.RootElement.GetProperty("userId").GetGuid(),
            email,
            json.RootElement.GetProperty("token").GetString()
            ?? throw new XunitException("Registration response did not contain a token."));
    }

    protected async Task<string> GetAdminTokenAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = GlinterApiFactory.AdminEmail,
            password = GlinterApiFactory.AdminPassword
        });
        response.EnsureSuccessStatusCode();
        var json = await ReadJsonAsync(response);
        return json.RootElement.GetProperty("token").GetString()
               ?? throw new XunitException("Admin login response did not contain a token.");
    }

    protected static HttpRequestMessage Request(
        HttpMethod method,
        string path,
        string? token = null,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    protected async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string? token = null,
        object? body = null)
    {
        using var request = Request(method, path, token, body);
        return await Client.SendAsync(request);
    }

    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    protected static async Task AssertProblemAsync(
        HttpResponseMessage response,
        int status,
        string errorCode)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var json = await ReadJsonAsync(response);
        Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(errorCode, json.RootElement.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            json.RootElement.GetProperty("traceId").GetString()));
    }

    protected async Task UpsertTravelerAsync(TestUser user)
    {
        var response = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/traveler",
            user.Token,
            new { displayName = $"Traveler {user.UserId}", interestIds = Array.Empty<Guid>() });
        response.EnsureSuccessStatusCode();
    }

    protected async Task UpsertHotelOwnerAsync(TestUser user)
    {
        var response = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/hotel-owner",
            user.Token,
            new { businessName = $"Hotel {user.UserId}" });
        response.EnsureSuccessStatusCode();
    }

    protected async Task UpsertProviderAsync(TestUser user)
    {
        var response = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/experience-provider",
            user.Token,
            new { businessName = $"Provider {user.UserId}" });
        response.EnsureSuccessStatusCode();
    }

    protected async Task<int> CreateRegionHierarchyAsync(string adminToken)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];

        var country = await SendAsync(
            HttpMethod.Post,
            "/api/regions/countries",
            adminToken,
            new { nameEn = $"Country {suffix}", pcode = $"C{suffix}" });
        country.EnsureSuccessStatusCode();
        using var countryJson = await ReadJsonAsync(country);
        var countryId = countryJson.RootElement.GetProperty("gid").GetInt32();

        var governorate = await SendAsync(
            HttpMethod.Post,
            "/api/regions/governorates",
            adminToken,
            new { adm0Gid = countryId, nameEn = $"Governorate {suffix}", pcode = $"G{suffix}" });
        governorate.EnsureSuccessStatusCode();
        using var governorateJson = await ReadJsonAsync(governorate);
        var governorateId = governorateJson.RootElement.GetProperty("gid").GetInt32();

        var district = await SendAsync(
            HttpMethod.Post,
            "/api/regions/districts",
            adminToken,
            new { adm1Gid = governorateId, nameEn = $"District {suffix}", pcode = $"D{suffix}" });
        district.EnsureSuccessStatusCode();
        using var districtJson = await ReadJsonAsync(district);
        var districtId = districtJson.RootElement.GetProperty("gid").GetInt32();

        var neighbourhood = await SendAsync(
            HttpMethod.Post,
            "/api/regions/neighbourhoods",
            adminToken,
            new { adm2Gid = districtId, nameEn = $"Neighbourhood {suffix}", pcode = $"N{suffix}" });
        neighbourhood.EnsureSuccessStatusCode();
        using var neighbourhoodJson = await ReadJsonAsync(neighbourhood);
        return neighbourhoodJson.RootElement.GetProperty("gid").GetInt32();
    }

    protected sealed record TestUser(Guid UserId, string Email, string Token);
}
