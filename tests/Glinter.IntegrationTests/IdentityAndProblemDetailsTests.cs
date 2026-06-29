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
    public async Task Administrative_identity_changes_are_audited_and_admin_only()
    {
        var target = await CreateUserAsync("Traveler", "audit-target");
        var otherTraveler = await CreateUserAsync("Traveler", "audit-reader");
        var adminToken = await GetAdminTokenAsync();

        var assignRole = await SendAsync(
            HttpMethod.Post,
            $"/api/admin/users/{target.UserId}/roles",
            adminToken,
            new { role = "LocalBuddy" });
        assignRole.EnsureSuccessStatusCode();

        var deactivate = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/users/{target.UserId}/status",
            adminToken,
            new { isActive = false });
        deactivate.EnsureSuccessStatusCode();

        var forbidden = await SendAsync(
            HttpMethod.Get,
            "/api/admin/audit-events",
            otherTraveler.Token);
        await AssertProblemAsync(forbidden, 403, "forbidden");

        var audit = await SendAsync(
            HttpMethod.Get,
            "/api/admin/audit-events?page=1&pageSize=100",
            adminToken);
        audit.EnsureSuccessStatusCode();
        using var auditJson = await ReadJsonAsync(audit);
        var items = auditJson.RootElement.GetProperty("items").EnumerateArray().ToList();

        var roleEvent = Assert.Single(
            items,
            item =>
                item.GetProperty("action").GetString() == "AdminUsers.AssignRole" &&
                item.GetProperty("target").GetString()!.Contains(
                    target.UserId.ToString(),
                    StringComparison.OrdinalIgnoreCase));
        Assert.True(roleEvent.GetProperty("succeeded").GetBoolean());
        Assert.Contains(
            roleEvent.GetProperty("changes")
                .GetProperty("before")
                .GetProperty("roles")
                .EnumerateArray(),
            role => role.GetString() == "Traveler");
        Assert.Contains(
            roleEvent.GetProperty("changes")
                .GetProperty("after")
                .GetProperty("roles")
                .EnumerateArray(),
            role => role.GetString() == "LocalBuddy");

        var statusEvent = Assert.Single(
            items,
            item =>
                item.GetProperty("action").GetString() == "AdminUsers.ChangeUserStatus" &&
                item.GetProperty("target").GetString()!.Contains(
                    target.UserId.ToString(),
                    StringComparison.OrdinalIgnoreCase));
        Assert.Equal(200, statusEvent.GetProperty("statusCode").GetInt32());
        Assert.NotEqual(
            System.Text.Json.JsonValueKind.Null,
            statusEvent.GetProperty("completedAtUtc").ValueKind);
        Assert.True(statusEvent.GetProperty("changes")
            .GetProperty("before")
            .GetProperty("isActive")
            .GetBoolean());
        Assert.False(statusEvent.GetProperty("changes")
            .GetProperty("after")
            .GetProperty("isActive")
            .GetBoolean());

        var unspecifiedDate = await SendAsync(
            HttpMethod.Get,
            "/api/admin/audit-events?fromUtc=2026-06-29T10:00:00",
            adminToken);
        await AssertProblemAsync(unspecifiedDate, 400, "validation_error");

        var utcDate = Uri.EscapeDataString(DateTime.UtcNow.AddDays(-1).ToString("O"));
        var validDate = await SendAsync(
            HttpMethod.Get,
            $"/api/admin/audit-events?fromUtc={utcDate}",
            adminToken);
        validDate.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Email_confirmation_and_password_reset_invalidate_existing_sessions()
    {
        var email = $"recovery.{Guid.NewGuid():N}@glinter.test";
        var registration = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Recovery User",
            email,
            password = GlinterApiFactory.UserPassword,
            role = "Traveler"
        });
        registration.EnsureSuccessStatusCode();
        using var registrationJson = await ReadJsonAsync(registration);

        var unconfirmedLogin = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = GlinterApiFactory.UserPassword
        });
        await AssertProblemAsync(unconfirmedLogin, 401, "authentication_required");

        var confirmation = await Client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            userId = registrationJson.RootElement.GetProperty("userId").GetGuid(),
            token = registrationJson.RootElement
                .GetProperty("developmentConfirmationToken").GetString()
        });
        confirmation.EnsureSuccessStatusCode();

        var login = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = GlinterApiFactory.UserPassword
        });
        login.EnsureSuccessStatusCode();
        using var loginJson = await ReadJsonAsync(login);
        var accessToken = loginJson.RootElement.GetProperty("token").GetString()!;
        var refreshToken = loginJson.RootElement.GetProperty("refreshToken").GetString()!;

        var forgot = await Client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email });
        forgot.EnsureSuccessStatusCode();
        using var forgotJson = await ReadJsonAsync(forgot);
        var resetToken = forgotJson.RootElement.GetProperty("developmentToken").GetString();

        const string newPassword = "ReplacementPassword!2026";
        var reset = await Client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email,
            token = resetToken,
            newPassword
        });
        reset.EnsureSuccessStatusCode();

        var oldAccess = await SendAsync(HttpMethod.Get, "/api/auth/me", accessToken);
        await AssertProblemAsync(oldAccess, 401, "authentication_required");
        var oldRefresh = await Client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken });
        await AssertProblemAsync(oldRefresh, 401, "authentication_required");

        var newLogin = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = newPassword
        });
        newLogin.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Refresh_tokens_rotate_and_reuse_revokes_the_whole_family()
    {
        var user = await CreateUserAsync("Traveler", "refresh");
        var rotation = await Client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = user.RefreshToken });
        rotation.EnsureSuccessStatusCode();
        using var rotationJson = await ReadJsonAsync(rotation);
        var rotatedAccess = rotationJson.RootElement.GetProperty("token").GetString()!;
        var rotatedRefresh = rotationJson.RootElement.GetProperty("refreshToken").GetString()!;

        var reuse = await Client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = user.RefreshToken });
        await AssertProblemAsync(reuse, 401, "authentication_required");

        var familyToken = await Client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = rotatedRefresh });
        await AssertProblemAsync(familyToken, 401, "authentication_required");

        var originalAccess = await SendAsync(HttpMethod.Get, "/api/auth/me", user.Token);
        await AssertProblemAsync(originalAccess, 401, "authentication_required");
        var replacementAccess = await SendAsync(HttpMethod.Get, "/api/auth/me", rotatedAccess);
        await AssertProblemAsync(replacementAccess, 401, "authentication_required");
    }

    [Fact]
    public async Task Admin_mfa_tickets_are_required_and_single_use()
    {
        await GetAdminTokenAsync();

        var login = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = GlinterApiFactory.AdminEmail,
            password = GlinterApiFactory.AdminPassword
        });
        login.EnsureSuccessStatusCode();
        using var loginJson = await ReadJsonAsync(login);
        Assert.True(loginJson.RootElement.GetProperty("requiresMfa").GetBoolean());
        Assert.False(loginJson.RootElement.GetProperty("requiresSetup").GetBoolean());
        var ticket = loginJson.RootElement.GetProperty("mfaTicket").GetString()!;
        var currentCode = GenerateTotp(Factory.AdminMfaSharedKey!);
        var invalidCode = currentCode == "000000" ? "000001" : "000000";

        var invalid = await Client.PostAsJsonAsync("/api/auth/mfa/verify", new
        {
            mfaTicket = ticket,
            code = invalidCode
        });
        await AssertProblemAsync(invalid, 401, "authentication_required");

        var replay = await Client.PostAsJsonAsync("/api/auth/mfa/verify", new
        {
            mfaTicket = ticket,
            code = GenerateTotp(Factory.AdminMfaSharedKey!)
        });
        await AssertProblemAsync(replay, 401, "authentication_required");

        var freshLogin = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = GlinterApiFactory.AdminEmail,
            password = GlinterApiFactory.AdminPassword
        });
        freshLogin.EnsureSuccessStatusCode();
        using var freshLoginJson = await ReadJsonAsync(freshLogin);
        var freshTicket = freshLoginJson.RootElement.GetProperty("mfaTicket").GetString()!;
        var verified = await Client.PostAsJsonAsync("/api/auth/mfa/verify", new
        {
            mfaTicket = freshTicket,
            code = GenerateTotp(Factory.AdminMfaSharedKey!)
        });
        verified.EnsureSuccessStatusCode();
        using var verifiedJson = await ReadJsonAsync(verified);
        Assert.False(string.IsNullOrWhiteSpace(
            verifiedJson.RootElement.GetProperty("token").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(
            verifiedJson.RootElement.GetProperty("refreshToken").GetString()));
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
