using Glinter.IntegrationTests.Infrastructure;
using Glinter.Shared.Infrastructure.Cleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class DashboardsMetricsAndCleanupTests : ApiTestBase
{
    public DashboardsMetricsAndCleanupTests(GlinterApiFactory factory) : base(factory)
    {
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
        var secondExpiredRevokedTokenId = Guid.NewGuid();
        var recentRevokedTokenId = Guid.NewGuid();
        var oldReadNotificationId = Guid.NewGuid();
        var oldUnreadNotificationId = Guid.NewGuid();
        var recentNotificationId = Guid.NewGuid();
        var oldCompletedAuditId = Guid.NewGuid();
        var oldPendingAuditId = Guid.NewGuid();
        var recentCompletedAuditId = Guid.NewGuid();

        await Factory.ExecuteAsync(
            $"""
             INSERT INTO revoked_tokens
                 ("Id", "Jti", "UserId", "RevokedAtUtc", "ExpiresAtUtc", "Reason")
             VALUES
                 ('{expiredRevokedTokenId}', '{Guid.NewGuid():N}', '{user.UserId}',
                  NOW() - INTERVAL '40 days', NOW() - INTERVAL '30 days', 'test'),
                 ('{secondExpiredRevokedTokenId}', '{Guid.NewGuid():N}', '{user.UserId}',
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

             INSERT INTO admin_audit_events
                 ("Id", "ActorUserId", "Action", "HttpMethod", "Path", "StatusCode",
                  "Succeeded", "CorrelationId", "CreatedAtUtc", "CompletedAtUtc")
             VALUES
                 ('{oldCompletedAuditId}', '{user.UserId}', 'Test.Old', 'PATCH',
                  '/test/old', 200, TRUE, 'old-complete',
                  NOW() - INTERVAL '900 days', NOW() - INTERVAL '900 days'),
                 ('{oldPendingAuditId}', '{user.UserId}', 'Test.Pending', 'PATCH',
                  '/test/pending', 102, FALSE, 'old-pending',
                  NOW() - INTERVAL '900 days', NULL),
                 ('{recentCompletedAuditId}', '{user.UserId}', 'Test.Recent', 'PATCH',
                  '/test/recent', 200, TRUE, 'recent-complete', NOW(), NOW());
             """);

        using var scope = Factory.Services.CreateScope();
        var cleanup = scope.ServiceProvider.GetRequiredService<DataCleanupService>();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<DataCleanupOptions>>()
            .Value;
        var originalBatchSize = options.BatchSize;
        var originalMaxBatches = options.MaxBatchesPerRun;

        try
        {
            await using (var lockConnection = new NpgsqlConnection(Factory.ConnectionString))
            {
                await lockConnection.OpenAsync();
                await using var acquireLock = new NpgsqlCommand(
                    """
                    SELECT pg_advisory_lock(
                        hashtextextended('glinter:data-cleanup', 0))
                    """,
                    lockConnection);
                await acquireLock.ExecuteScalarAsync();

                var skipped = await cleanup.RunOnceAsync();
                Assert.True(skipped.SkippedDueToLock);
            }

            options.BatchSize = 1;
            options.MaxBatchesPerRun = 1;
            var firstResult = await cleanup.RunOnceAsync();
            Assert.False(firstResult.SkippedDueToLock);
            Assert.Equal(1, firstResult.RevokedTokens);
            Assert.True(firstResult.ReadNotifications >= 1);
            Assert.True(firstResult.UnreadNotifications >= 1);
            Assert.Equal(1, firstResult.AdminAuditEvents);

            var expiredAfterFirstBatch = await Factory.ScalarAsync<long>(
                $"""
                 SELECT count(*) FROM revoked_tokens
                 WHERE "Id" IN ('{expiredRevokedTokenId}', '{secondExpiredRevokedTokenId}')
                 """);
            Assert.Equal(1, expiredAfterFirstBatch);

            var secondResult = await cleanup.RunOnceAsync();
            Assert.Equal(1, secondResult.RevokedTokens);

            var expiredCount = await Factory.ScalarAsync<long>(
                $"""
                 SELECT count(*) FROM revoked_tokens
                 WHERE "Id" IN ('{expiredRevokedTokenId}', '{secondExpiredRevokedTokenId}')
                 """);
            var recentTokenCount = await Factory.ScalarAsync<long>(
                $"""SELECT count(*) FROM revoked_tokens WHERE "Id" = '{recentRevokedTokenId}'""");
            var recentNotificationCount = await Factory.ScalarAsync<long>(
                $"""SELECT count(*) FROM notifications WHERE "Id" = '{recentNotificationId}'""");
            var oldCompletedAuditCount = await Factory.ScalarAsync<long>(
                $"""
                 SELECT count(*) FROM admin_audit_events
                 WHERE "Id" = '{oldCompletedAuditId}'
                 """);
            var preservedAuditCount = await Factory.ScalarAsync<long>(
                $"""
                 SELECT count(*) FROM admin_audit_events
                 WHERE "Id" IN ('{oldPendingAuditId}', '{recentCompletedAuditId}')
                 """);
            Assert.Equal(0, expiredCount);
            Assert.Equal(1, recentTokenCount);
            Assert.Equal(1, recentNotificationCount);
            Assert.Equal(0, oldCompletedAuditCount);
            Assert.Equal(2, preservedAuditCount);
        }
        finally
        {
            options.BatchSize = originalBatchSize;
            options.MaxBatchesPerRun = originalMaxBatches;
        }
    }
}
