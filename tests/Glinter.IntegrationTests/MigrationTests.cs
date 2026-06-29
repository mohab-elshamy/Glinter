using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class MigrationTests : ApiTestBase
{
    private const string PreviousStayMigration =
        "20260627000000_AllowCancelledStayRebooking";
    private const string PreviousExperienceMigration =
        "20260628001745_AddExperienceModerationHistory";

    public MigrationTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Search_indexes_are_valid_and_migrations_can_roll_back_and_reapply()
    {
        Assert.True(await ExtensionExistsAsync("pg_trgm"));
        Assert.True(await IndexIsReadyAsync("IX_stays_Name_trgm"));
        Assert.True(await IndexIsReadyAsync("IX_experiences_Title_trgm"));

        using var scope = Factory.Services.CreateScope();
        var stays = scope.ServiceProvider.GetRequiredService<StaysDbContext>();
        var experiences =
            scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>();
        var stayMigrator = stays.Database.GetService<IMigrator>();
        var experienceMigrator = experiences.Database.GetService<IMigrator>();

        try
        {
            await stayMigrator.MigrateAsync(PreviousStayMigration);
            Assert.False(await IndexExistsAsync("IX_stays_Name_trgm"));

            var pendingStay = await Client.GetAsync("/health/ready");
            Assert.Equal(
                HttpStatusCode.ServiceUnavailable,
                pendingStay.StatusCode);

            await stayMigrator.MigrateAsync();
            Assert.True(await IndexIsReadyAsync("IX_stays_Name_trgm"));

            await experienceMigrator.MigrateAsync(PreviousExperienceMigration);
            Assert.False(await IndexExistsAsync("IX_experiences_Title_trgm"));

            var pendingExperience = await Client.GetAsync("/health/ready");
            Assert.Equal(
                HttpStatusCode.ServiceUnavailable,
                pendingExperience.StatusCode);

            await experienceMigrator.MigrateAsync();
            Assert.True(await IndexIsReadyAsync("IX_experiences_Title_trgm"));

            var ready = await Client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        }
        finally
        {
            await stayMigrator.MigrateAsync();
            await experienceMigrator.MigrateAsync();
        }
    }

    private async Task<bool> ExtensionExistsAsync(string extensionName) =>
        await Factory.ScalarAsync<bool>(
            $"""
             SELECT EXISTS (
                 SELECT 1
                 FROM pg_extension
                 WHERE extname = '{extensionName}'
             )
             """);

    private async Task<bool> IndexExistsAsync(string indexName) =>
        await Factory.ScalarAsync<bool>(
            $"""
             SELECT EXISTS (
                 SELECT 1
                 FROM pg_class
                 WHERE relkind = 'i' AND relname = '{indexName}'
             )
             """);

    private async Task<bool> IndexIsReadyAsync(string indexName) =>
        await Factory.ScalarAsync<bool>(
            $"""
             SELECT COALESCE(bool_and(i.indisvalid AND i.indisready), FALSE)
             FROM pg_index AS i
             INNER JOIN pg_class AS c ON c.oid = i.indexrelid
             WHERE c.relname = '{indexName}'
             """);
}
