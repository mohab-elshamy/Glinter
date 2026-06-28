using Glinter.IntegrationTests.Infrastructure;
using Glinter.Shared.Infrastructure.Cleanup;
using Microsoft.Extensions.DependencyInjection;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class DashboardsMetricsAndCleanupTests : ApiTestBase
{
    public DashboardsMetricsAndCleanupTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Dashboards_and_analytics_are_role_scoped_and_return_real_aggregates()
    {
        var adminToken = await GetAdminTokenAsync();
        var traveler = await CreateUserAsync("Traveler", "dashboard-traveler");
        var hotelOwner = await CreateUserAsync("HotelOwner", "dashboard-hotel");
        var provider = await CreateUserAsync(
            "ExperienceProvider",
            "dashboard-experience");
        await UpsertHotelOwnerAsync(hotelOwner);
        await UpsertProviderAsync(provider);

        var forbidden = await SendAsync(
            HttpMethod.Get,
            "/api/dashboard/admin",
            traveler.Token);
        await AssertProblemAsync(forbidden, 403, "forbidden");

        var admin = await SendAsync(
            HttpMethod.Get,
            "/api/dashboard/admin",
            adminToken);
        admin.EnsureSuccessStatusCode();
        using var adminJson = await ReadJsonAsync(admin);
        Assert.True(adminJson.RootElement.GetProperty("totalUsers").GetInt32() >= 4);
        Assert.True(adminJson.RootElement.GetProperty("activeUsers").GetInt32() >= 4);

        var owner = await SendAsync(
            HttpMethod.Get,
            "/api/dashboard/hotel-owner",
            hotelOwner.Token);
        owner.EnsureSuccessStatusCode();
        using var ownerJson = await ReadJsonAsync(owner);
        Assert.Equal(0, ownerJson.RootElement.GetProperty("totalStays").GetInt32());
        Assert.Equal(0, ownerJson.RootElement.GetProperty("totalBookings").GetInt32());
        Assert.Equal(
            0,
            ownerJson.RootElement.GetProperty("grossBookingValue").GetArrayLength());
        Assert.False(ownerJson.RootElement.TryGetProperty("revenue", out _));

        var experienceProvider = await SendAsync(
            HttpMethod.Get,
            "/api/dashboard/experience-provider",
            provider.Token);
        experienceProvider.EnsureSuccessStatusCode();
        using var providerJson = await ReadJsonAsync(experienceProvider);
        Assert.Equal(
            0,
            providerJson.RootElement.GetProperty("totalExperiences").GetInt32());
        Assert.Equal(
            0,
            providerJson.RootElement.GetProperty("grossBookingValue").GetArrayLength());
        Assert.False(providerJson.RootElement.TryGetProperty("revenue", out _));

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var analytics = await SendAsync(
            HttpMethod.Get,
            $"/api/admin/analytics?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}",
            adminToken);
        analytics.EnsureSuccessStatusCode();
        using var analyticsJson = await ReadJsonAsync(analytics);
        Assert.Equal(2, analyticsJson.RootElement
            .GetProperty("dailyActivity").GetArrayLength());
        Assert.True(analyticsJson.RootElement.GetProperty("newUsers").GetInt32() >= 3);
        Assert.True(analyticsJson.RootElement.TryGetProperty("grossBookingValue", out _));
        Assert.False(analyticsJson.RootElement.TryGetProperty("revenue", out _));
    }

    [Fact]
    public async Task Metrics_are_admin_only_and_use_bounded_prometheus_labels()
    {
        var traveler = await CreateUserAsync("Traveler", "metrics");
        await Client.GetAsync("/health/live");

        var forbidden = await SendAsync(HttpMethod.Get, "/metrics", traveler.Token);
        await AssertProblemAsync(forbidden, 403, "forbidden");

        var adminToken = await GetAdminTokenAsync();
        var metrics = await SendAsync(HttpMethod.Get, "/metrics", adminToken);
        metrics.EnsureSuccessStatusCode();
        var body = await metrics.Content.ReadAsStringAsync();
        Assert.Contains("glinter_http_requests_total", body);
        Assert.Contains("glinter_http_request_duration_milliseconds_bucket", body);
        Assert.Contains("glinter_cleanup_runs_total", body);
        Assert.DoesNotContain(traveler.UserId.ToString(), body);
        Assert.DoesNotContain("/api/", body);
    }

    [Fact]
    public async Task Cleanup_removes_expired_records_and_preserves_recent_records()
    {
        var user = await CreateUserAsync("Traveler", "cleanup");
        var expiredRevokedTokenId = Guid.NewGuid();
        var recentRevokedTokenId = Guid.NewGuid();
        var oldReadNotificationId = Guid.NewGuid();
        var oldUnreadNotificationId = Guid.NewGuid();
        var recentNotificationId = Guid.NewGuid();

        await Factory.ExecuteAsync(
            $"""
             INSERT INTO revoked_tokens
                 ("Id", "Jti", "UserId", "RevokedAtUtc", "ExpiresAtUtc", "Reason")
             VALUES
                 ('{expiredRevokedTokenId}', '{Guid.NewGuid():N}', '{user.UserId}',
                  NOW() - INTERVAL '40 days', NOW() - INTERVAL '30 days', 'test'),
                 ('{recentRevokedTokenId}', '{Guid.NewGuid():N}', '{user.UserId}',
                  NOW(), NOW() + INTERVAL '1 day', 'test');

             INSERT INTO notifications
                 ("Id", "UserId", "Type", "Title", "Body", "CreatedAtUtc", "ReadAtUtc")
             VALUES
                 ('{oldReadNotificationId}', '{user.UserId}', 'System',
                  'old read', 'cleanup', NOW() - INTERVAL '120 days',
                  NOW() - INTERVAL '120 days'),
                 ('{oldUnreadNotificationId}', '{user.UserId}', 'System',
                  'old unread', 'cleanup', NOW() - INTERVAL '400 days', NULL),
                 ('{recentNotificationId}', '{user.UserId}', 'System',
                  'recent', 'keep', NOW(), NULL);
             """);

        using var scope = Factory.Services.CreateScope();
        var cleanup = scope.ServiceProvider.GetRequiredService<DataCleanupService>();
        var result = await cleanup.RunOnceAsync();
        Assert.True(result.RevokedTokens >= 1);
        Assert.True(result.ReadNotifications >= 1);
        Assert.True(result.UnreadNotifications >= 1);

        var expiredCount = await Factory.ScalarAsync<long>(
            $"""SELECT count(*) FROM revoked_tokens WHERE "Id" = '{expiredRevokedTokenId}'""");
        var recentTokenCount = await Factory.ScalarAsync<long>(
            $"""SELECT count(*) FROM revoked_tokens WHERE "Id" = '{recentRevokedTokenId}'""");
        var recentNotificationCount = await Factory.ScalarAsync<long>(
            $"""SELECT count(*) FROM notifications WHERE "Id" = '{recentNotificationId}'""");
        Assert.Equal(0, expiredCount);
        Assert.Equal(1, recentTokenCount);
        Assert.Equal(1, recentNotificationCount);
    }
}
