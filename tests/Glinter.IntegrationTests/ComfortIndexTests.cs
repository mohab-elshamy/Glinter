using Glinter.IntegrationTests.Infrastructure;
using Glinter.Modules.Experiences.Domain.Entities;
using Glinter.Modules.Experiences.Domain.Enums;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.Stays.Domain.Entities;
using Glinter.Modules.Stays.Domain.Enums;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Glinter.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class ComfortIndexTests : ApiTestBase
{
    public ComfortIndexTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Comfort_index_calculates_from_stay_and_experience_ratings()
    {
        const int targetAdm2 = 720001;
        const int otherAdm2 = 720002;

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var staysDbContext =
                scope.ServiceProvider.GetRequiredService<StaysDbContext>();
            var experiencesDbContext =
                scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>();

            staysDbContext.Stays.AddRange(
                new Stay
                {
                    Name = "Comfort Stay Target",
                    SourceType = StaySourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    Rating = 4.5m,
                    Reviews = 100
                },
                new Stay
                {
                    Name = "Comfort Stay Other",
                    SourceType = StaySourceType.ThirdParty,
                    Adm2Gid = otherAdm2,
                    Rating = 3.5m,
                    Reviews = 50
                },
                new Stay
                {
                    Name = "Comfort Stay Invalid Reviews",
                    SourceType = StaySourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    Rating = 5m,
                    Reviews = 1
                });

            experiencesDbContext.Experiences.AddRange(
                new Experience
                {
                    Name = "Comfort Experience Target",
                    Category = ExperienceCategory.Historical,
                    SourceType = ExperienceSourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    Rating = 4m,
                    Reviews = 25
                },
                new Experience
                {
                    Name = "Comfort Experience Other",
                    Category = ExperienceCategory.Nature,
                    SourceType = ExperienceSourceType.ThirdParty,
                    Adm2Gid = otherAdm2,
                    Rating = 4m,
                    Reviews = 100
                },
                new Experience
                {
                    Name = "Comfort Experience Pending",
                    Category = ExperienceCategory.Dining,
                    SourceType = ExperienceSourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    Rating = 5m,
                    Reviews = 1000,
                    ModerationStatus = ExperienceModerationStatus.Pending
                });

            await staysDbContext.SaveChangesAsync();
            await experiencesDbContext.SaveChangesAsync();
        }

        var response = await Client.GetAsync($"/api/comfort-index/adm2/{targetAdm2}");
        response.EnsureSuccessStatusCode();

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal(
            targetAdm2,
            root.GetProperty("filters").GetProperty("adm2Gid").GetInt32());

        var stays = root.GetProperty("stays");
        Assert.Equal(1, stays.GetProperty("sampleSize").GetInt32());
        Assert.Equal(20.72m, stays.GetProperty("averageScore").GetDecimal());
        Assert.True(stays.GetProperty("indexValue").GetDecimal() > 0);

        var experiences = root.GetProperty("experiences");
        Assert.Equal(1, experiences.GetProperty("sampleSize").GetInt32());
        Assert.Equal(12.88m, experiences.GetProperty("averageScore").GetDecimal());
        Assert.True(experiences.GetProperty("indexValue").GetDecimal() > 0);

        var combined = root.GetProperty("combined");
        Assert.Equal(2, combined.GetProperty("sampleSize").GetInt32());
        Assert.Equal(16.8m, combined.GetProperty("averageScore").GetDecimal());
        Assert.True(combined.GetProperty("indexValue").GetDecimal() > 0);
    }
}
