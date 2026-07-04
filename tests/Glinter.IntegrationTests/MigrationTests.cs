using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Glinter.Modules.SafetyIndex.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Glinter.Modules.Buddy.Infrastructure.Persistence;
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

        Assert.Empty(await identity.Database.GetPendingMigrationsAsync());
        Assert.Empty(await profiles.Database.GetPendingMigrationsAsync());
        Assert.Empty(await buddy.Database.GetPendingMigrationsAsync());
        Assert.Empty(await regions.Database.GetPendingMigrationsAsync());
        Assert.Empty(await safety.Database.GetPendingMigrationsAsync());
        Assert.Empty(await stays.Database.GetPendingMigrationsAsync());
        Assert.Empty(await experiences.Database.GetPendingMigrationsAsync());
        Assert.Empty(await communication.Database.GetPendingMigrationsAsync());
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
}
