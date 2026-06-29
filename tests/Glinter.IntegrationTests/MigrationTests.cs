using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
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
    public async Task Current_stays_and_experiences_models_are_fully_migrated()
    {
        using var scope = Factory.Services.CreateScope();
        var stays = scope.ServiceProvider.GetRequiredService<StaysDbContext>();
        var experiences =
            scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>();

        Assert.Empty(await stays.Database.GetPendingMigrationsAsync());
        Assert.Empty(await experiences.Database.GetPendingMigrationsAsync());
        Assert.True(await TableExistsAsync("stays", "stays"));
        Assert.True(await TableExistsAsync("experiences", "experiences"));

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
