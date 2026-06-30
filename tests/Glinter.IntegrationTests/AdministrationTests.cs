using Glinter.IntegrationTests.Infrastructure;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class AdministrationTests : ApiTestBase
{
    public AdministrationTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Admin_can_verify_buddies_and_moderate_experiences()
    {
        var buddy = await CreateUserAsync("LocalBuddy", "admin-buddy");
        var provider = await CreateUserAsync("ExperienceProvider", "admin-provider");
        var adminToken = await GetAdminTokenAsync();

        var buddyProfile = await SendAsync(
            HttpMethod.Put,
            "/api/profiles/local-buddy",
            buddy.Token,
            new
            {
                displayName = "Pending Buddy",
                city = "Cairo",
                interestIds = Array.Empty<Guid>()
            });
        buddyProfile.EnsureSuccessStatusCode();

        var pendingBuddies = await SendAsync(
            HttpMethod.Get,
            "/api/admin/local-buddies?verificationStatus=Pending",
            adminToken);
        pendingBuddies.EnsureSuccessStatusCode();
        using var pendingBuddiesJson = await ReadJsonAsync(pendingBuddies);
        Assert.Contains(
            pendingBuddiesJson.RootElement.EnumerateArray(),
            item => item.GetProperty("userId").GetGuid() == buddy.UserId);

        var approveBuddy = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/local-buddies/{buddy.UserId}/verification",
            adminToken,
            new { verificationStatus = "Approved" });
        approveBuddy.EnsureSuccessStatusCode();
        var buddyNotifications = await SendAsync(
            HttpMethod.Get,
            "/api/notifications?page=1&pageSize=20",
            buddy.Token);
        buddyNotifications.EnsureSuccessStatusCode();
        using var buddyNotificationsJson = await ReadJsonAsync(buddyNotifications);
        Assert.Contains(
            buddyNotificationsJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Moderation" &&
                    item.GetProperty("sourceEntityType").GetString() == "LocalBuddyVerification");

        await UpsertProviderAsync(provider);
        var createExperience = await SendAsync(
            HttpMethod.Post,
            "/api/experiences",
            provider.Token,
            new
            {
                category = "Historical",
                name = $"Pending Experience {Guid.NewGuid():N}",
                address = "Cairo",
                featuredImageLinks = Array.Empty<string>(),
                hours = Array.Empty<object>(),
                popularTimes = Array.Empty<object>(),
                amenities = Array.Empty<string>()
            });
        Assert.Equal(HttpStatusCode.Created, createExperience.StatusCode);
        using var experienceJson = await ReadJsonAsync(createExperience);
        var experienceId = experienceJson.RootElement.GetProperty("id").GetInt32();
        Assert.Equal(
            "Pending",
            experienceJson.RootElement.GetProperty("moderationStatus").GetString());
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await Client.GetAsync($"/api/experiences/{experienceId}")).StatusCode);

        var pendingExperiences = await SendAsync(
            HttpMethod.Get,
            "/api/admin/experiences?moderationStatus=Pending",
            adminToken);
        pendingExperiences.EnsureSuccessStatusCode();
        using var pendingExperiencesJson = await ReadJsonAsync(pendingExperiences);
        Assert.Contains(
            pendingExperiencesJson.RootElement.EnumerateArray(),
            item => item.GetProperty("id").GetInt32() == experienceId);

        var approveExperience = await SendAsync(
            HttpMethod.Patch,
            $"/api/admin/experiences/{experienceId}/moderation",
            adminToken,
            new { moderationStatus = "Approved" });
        approveExperience.EnsureSuccessStatusCode();
        (await Client.GetAsync($"/api/experiences/{experienceId}")).EnsureSuccessStatusCode();
        var providerNotifications = await SendAsync(
            HttpMethod.Get,
            "/api/notifications?page=1&pageSize=20",
            provider.Token);
        providerNotifications.EnsureSuccessStatusCode();
        using var providerNotificationsJson = await ReadJsonAsync(providerNotifications);
        Assert.Contains(
            providerNotificationsJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("type").GetString() == "Moderation" &&
                    item.GetProperty("sourceEntityType").GetString() == "Experience");

        var audit = await SendAsync(
            HttpMethod.Get,
            "/api/admin/audit-events?page=1&pageSize=100",
            adminToken);
        audit.EnsureSuccessStatusCode();
        using var auditJson = await ReadJsonAsync(audit);
        Assert.Contains(
            auditJson.RootElement.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("action").GetString() == "AdminExperiences.Moderate" &&
                    item.GetProperty("target").GetString()!.Contains(
                        experienceId.ToString(),
                        StringComparison.Ordinal));
    }

    [Fact]
    public async Task Admin_dashboard_and_analytics_are_live_and_admin_only()
    {
        var traveler = await CreateUserAsync("Traveler", "admin-dashboard-forbidden");
        var forbidden = await SendAsync(
            HttpMethod.Get,
            "/api/admin/dashboard",
            traveler.Token);
        await AssertProblemAsync(forbidden, 403, "forbidden");

        var adminToken = await GetAdminTokenAsync();
        var dashboard = await SendAsync(
            HttpMethod.Get,
            "/api/admin/dashboard",
            adminToken);
        dashboard.EnsureSuccessStatusCode();
        using var dashboardJson = await ReadJsonAsync(dashboard);
        Assert.True(dashboardJson.RootElement.GetProperty("totalUsers").GetInt32() > 0);
        Assert.True(dashboardJson.RootElement.GetProperty("activeUsers").GetInt32() > 0);

        var analytics = await SendAsync(
            HttpMethod.Get,
            "/api/admin/analytics",
            adminToken);
        analytics.EnsureSuccessStatusCode();
        using var analyticsJson = await ReadJsonAsync(analytics);
        Assert.True(analyticsJson.RootElement.GetProperty("usersByRole")
            .TryGetProperty("Traveler", out _));
        Assert.True(analyticsJson.RootElement.TryGetProperty("bookingsByStatus", out _));
        Assert.True(analyticsJson.RootElement.TryGetProperty("listingsByType", out _));
    }
}
