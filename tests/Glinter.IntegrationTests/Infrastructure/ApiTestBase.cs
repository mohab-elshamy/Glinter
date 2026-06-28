using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
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

        using var json = await ReadJsonAsync(response);
        var userId = json.RootElement.GetProperty("userId").GetGuid();
        var confirmationToken = json.RootElement
            .GetProperty("developmentConfirmationToken")
            .GetString() ?? throw new XunitException(
                "Registration response did not contain a development confirmation token.");

        var confirmation = await Client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            userId,
            token = confirmationToken
        });
        confirmation.EnsureSuccessStatusCode();

        var login = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = GlinterApiFactory.UserPassword
        });
        login.EnsureSuccessStatusCode();
        using var loginJson = await ReadJsonAsync(login);

        return new TestUser(
            userId,
            email,
            loginJson.RootElement.GetProperty("token").GetString()
            ?? throw new XunitException("Login response did not contain an access token."),
            loginJson.RootElement.GetProperty("refreshToken").GetString()
            ?? throw new XunitException("Login response did not contain a refresh token."));
    }

    protected async Task<string> GetAdminTokenAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = GlinterApiFactory.AdminEmail,
            password = GlinterApiFactory.AdminPassword
        });
        response.EnsureSuccessStatusCode();
        using var json = await ReadJsonAsync(response);
        var ticket = json.RootElement.GetProperty("mfaTicket").GetString()
                     ?? throw new XunitException("Admin login did not contain an MFA ticket.");
        var requiresSetup = json.RootElement.GetProperty("requiresSetup").GetBoolean();

        if (requiresSetup)
        {
            var setup = await Client.PostAsJsonAsync("/api/auth/mfa/setup", new
            {
                mfaTicket = ticket
            });
            setup.EnsureSuccessStatusCode();
            using var setupJson = await ReadJsonAsync(setup);
            Factory.AdminMfaSharedKey = setupJson.RootElement
                .GetProperty("sharedKey")
                .GetString() ?? throw new XunitException("MFA setup did not return a key.");
            ticket = setupJson.RootElement.GetProperty("mfaTicket").GetString()
                     ?? throw new XunitException("MFA setup did not return an enable ticket.");

            var enable = await Client.PostAsJsonAsync("/api/auth/mfa/enable", new
            {
                mfaTicket = ticket,
                code = GenerateTotp(Factory.AdminMfaSharedKey)
            });
            enable.EnsureSuccessStatusCode();
            using var enableJson = await ReadJsonAsync(enable);
            return enableJson.RootElement.GetProperty("authentication")
                       .GetProperty("token").GetString()
                   ?? throw new XunitException("MFA enable did not return an access token.");
        }

        var sharedKey = Factory.AdminMfaSharedKey
                        ?? throw new XunitException("Admin MFA key is unavailable.");
        var verify = await Client.PostAsJsonAsync("/api/auth/mfa/verify", new
        {
            mfaTicket = ticket,
            code = GenerateTotp(sharedKey)
        });
        verify.EnsureSuccessStatusCode();
        using var verifyJson = await ReadJsonAsync(verify);
        return verifyJson.RootElement.GetProperty("token").GetString()
               ?? throw new XunitException("MFA verification did not return an access token.");
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

    protected static string GenerateTotp(string base32Secret)
    {
        var key = DecodeBase32(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        Span<byte> counterBytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

        var hash = HMACSHA1.HashData(key, counterBytes);
        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);
        return (binaryCode % 1_000_000).ToString("D6");
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var character in value.TrimEnd('=').ToUpperInvariant())
        {
            var index = alphabet.IndexOf(character);
            if (index < 0)
                throw new XunitException("Authenticator key is not valid Base32.");

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft < 8)
                continue;

            output.Add((byte)(buffer >> (bitsLeft - 8)));
            bitsLeft -= 8;
            buffer &= (1 << bitsLeft) - 1;
        }

        return output.ToArray();
    }

    protected sealed record TestUser(
        Guid UserId,
        string Email,
        string Token,
        string RefreshToken);
}
