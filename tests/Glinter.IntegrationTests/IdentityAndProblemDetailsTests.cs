using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class IdentityAndProblemDetailsTests : ApiTestBase
{
    public IdentityAndProblemDetailsTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Identity_lockout_revocation_and_inactive_tokens_are_enforced()
    {
        var lockoutUser = await CreateUserAsync("Traveler", "lockout");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var failedLogin = await Client.PostAsJsonAsync("/api/auth/login", new
            {
                email = lockoutUser.Email,
                password = "WrongPassword!2026"
            });
            await AssertProblemAsync(failedLogin, 401, "authentication_required");
        }

        var lockedLogin = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = lockoutUser.Email,
            password = GlinterApiFactory.UserPassword
        });
        await AssertProblemAsync(lockedLogin, 401, "authentication_required");

        var revokedUser = await CreateUserAsync("Traveler", "revoked");
        var logout = await SendAsync(HttpMethod.Post, "/api/auth/logout", revokedUser.Token);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        var revokedMe = await SendAsync(HttpMethod.Get, "/api/auth/me", revokedUser.Token);
        await AssertProblemAsync(revokedMe, 401, "authentication_required");

        var inactiveUser = await CreateUserAsync("Traveler", "inactive");
        var adminToken = await GetAdminTokenAsync();
        var deactivate = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/users/{inactiveUser.UserId}/status",
            adminToken,
            new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var inactiveMe = await SendAsync(HttpMethod.Get, "/api/auth/me", inactiveUser.Token);
        await AssertProblemAsync(inactiveMe, 401, "authentication_required");
    }

    [Fact]
    public async Task Public_admin_registration_is_rejected()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Blocked Admin",
            email = $"blocked-admin.{Guid.NewGuid():N}@glinter.test",
            password = GlinterApiFactory.UserPassword,
            role = "Admin"
        });

        await AssertProblemAsync(response, 400, "validation_error");
    }

    [Fact]
    public async Task Common_client_failures_use_problem_details()
    {
        var validation = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = string.Empty,
            password = string.Empty
        });
        await AssertProblemAsync(validation, 400, "validation_error");

        var unauthenticated = await Client.GetAsync("/api/auth/me");
        await AssertProblemAsync(unauthenticated, 401, "authentication_required");

        var traveler = await CreateUserAsync("Traveler", "forbidden");
        var forbidden = await SendAsync(
            HttpMethod.Get,
            "/api/admin/users",
            traveler.Token);
        await AssertProblemAsync(forbidden, 403, "forbidden");

        var missing = await Client.GetAsync($"/api/stays/{Guid.NewGuid()}");
        await AssertProblemAsync(missing, 404, "not_found");

        var duplicateEmail = $"duplicate.{Guid.NewGuid():N}@glinter.test";
        var registration = new
        {
            fullName = "Duplicate User",
            email = duplicateEmail,
            password = GlinterApiFactory.UserPassword,
            role = "Traveler"
        };
        var first = await Client.PostAsJsonAsync("/api/auth/register", registration);
        first.EnsureSuccessStatusCode();
        var duplicate = await Client.PostAsJsonAsync("/api/auth/register", registration);
        await AssertProblemAsync(duplicate, 409, "conflict");
    }
}
