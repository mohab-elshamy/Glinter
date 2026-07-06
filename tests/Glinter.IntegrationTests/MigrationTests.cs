using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Glinter.Modules.SafetyIndex.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Glinter.Modules.Buddy.Infrastructure.Persistence;
using Glinter.Modules.Itineraries.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class MigrationTests : ApiTestBase
{
    public MigrationTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task All_module_models_are_fully_migrated()
    {
        using var scope = Factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
        var profiles = scope.ServiceProvider.GetRequiredService<ProfilesDbContext>();
        var buddy = scope.ServiceProvider.GetRequiredService<BuddyDbContext>();
        var regions = scope.ServiceProvider.GetRequiredService<RegionsDbContext>();
        var safety = scope.ServiceProvider.GetRequiredService<SafetyIndexDbContext>();
        var stays = scope.ServiceProvider.GetRequiredService<StaysDbContext>();
        var experiences =
            scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>();
        var communication =
            scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        var itineraries =
            scope.ServiceProvider.GetRequiredService<ItinerariesDbContext>();

        Assert.Empty(await identity.Database.GetPendingMigrationsAsync());
        Assert.Empty(await profiles.Database.GetPendingMigrationsAsync());
        Assert.Empty(await buddy.Database.GetPendingMigrationsAsync());
        Assert.Empty(await regions.Database.GetPendingMigrationsAsync());
        Assert.Empty(await safety.Database.GetPendingMigrationsAsync());
        Assert.Empty(await stays.Database.GetPendingMigrationsAsync());
        Assert.Empty(await experiences.Database.GetPendingMigrationsAsync());
        Assert.Empty(await communication.Database.GetPendingMigrationsAsync());
        Assert.Empty(await itineraries.Database.GetPendingMigrationsAsync());
        Assert.False(identity.Database.HasPendingModelChanges());
        Assert.False(profiles.Database.HasPendingModelChanges());
        Assert.False(regions.Database.HasPendingModelChanges());
        Assert.False(safety.Database.HasPendingModelChanges());
        Assert.False(stays.Database.HasPendingModelChanges());
        Assert.False(experiences.Database.HasPendingModelChanges());
        Assert.False(communication.Database.HasPendingModelChanges());
        Assert.False(itineraries.Database.HasPendingModelChanges());
        Assert.True(await TableExistsAsync("public", "users"));
        Assert.True(await TableExistsAsync("public", "traveler_profiles"));
        Assert.True(await TableExistsAsync("public", "buddy_availability"));
        Assert.True(await TableExistsAsync("public", "buddy_bookings"));
        Assert.True(await TableExistsAsync("public", "buddy_reviews"));
        Assert.True(await TableExistsAsync("public", "adm0"));
        Assert.True(await TableExistsAsync("public", "safety_index_results"));
        Assert.True(await TableExistsAsync("stays", "stays"));
        Assert.True(await TableExistsAsync("experiences", "experiences"));
        Assert.True(await TableExistsAsync("public", "chat_threads"));
        Assert.True(await TableExistsAsync("itineraries", "saved_itineraries"));

        Assert.Equal(3, await Factory.ScalarAsync<long>(
            """
            SELECT count(*)
            FROM information_schema.tables
            WHERE table_name IN ('buddy_availability', 'buddy_bookings', 'buddy_reviews')
            """));
        Assert.True(await UniqueIndexExistsAsync("public", "users", "NormalizedEmail"));
        Assert.True(await UniqueIndexExistsAsync("public", "chat_threads", "DirectKey"));
        Assert.True(await UniqueIndexExistsAsync("public", "buddy_reviews", "BookingId"));
        Assert.True(await UniqueIndexExistsAsync("stays", "stay_reviews", "BookingId"));
        Assert.True(await UniqueIndexExistsAsync(
            "experiences", "experience_reviews", "BookingId"));
        Assert.True(await PrimaryKeyContainsAsync(
            "public", "user_follows", "FollowerUserId", "FollowedUserId"));
        Assert.True(await PrimaryKeyContainsAsync(
            "public", "experience_favorites", "UserId", "ExperienceId"));

        var ready = await Client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    private async Task<bool> TableExistsAsync(string schema, string table) =>
        await Factory.ScalarAsync<bool>(
            $"""
             SELECT EXISTS (
                 SELECT 1
                 FROM information_schema.tables
                 WHERE table_schema = '{schema}'
                   AND table_name = '{table}'
             )
             """);

    private async Task<bool> UniqueIndexExistsAsync(
        string schema,
        string table,
        string column) =>
        await Factory.ScalarAsync<bool>(
            $"""
             SELECT EXISTS (
                 SELECT 1
                 FROM pg_indexes
                 WHERE schemaname = '{schema}'
                   AND tablename = '{table}'
                   AND indexdef ILIKE '%UNIQUE%'
                   AND indexdef ILIKE '%"{column}"%'
             )
             """);

    private async Task<bool> PrimaryKeyContainsAsync(
        string schema,
        string table,
        params string[] columns)
    {
        var requiredColumns = string.Join(
            ", ",
            columns.Select(column => $"'{column}'"));
        return await Factory.ScalarAsync<bool>(
            $"""
             SELECT (
                 SELECT count(DISTINCT a.attname)
                 FROM pg_constraint c
                 JOIN pg_class t ON t.oid = c.conrelid
                 JOIN pg_namespace n ON n.oid = t.relnamespace
                 JOIN unnest(c.conkey) AS key(attnum) ON TRUE
                 JOIN pg_attribute a
                   ON a.attrelid = t.oid AND a.attnum = key.attnum
                 WHERE c.contype = 'p'
                   AND n.nspname = '{schema}'
                   AND t.relname = '{table}'
                   AND a.attname IN ({requiredColumns})
             ) = {columns.Length}
             """);
    }
}
