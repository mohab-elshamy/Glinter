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
public sealed class PriceIndexTests : ApiTestBase
{
    public PriceIndexTests(GlinterApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Price_index_calculates_from_stay_and_experience_prices()
    {
        const int targetAdm2 = 710001;
        const int otherAdm2 = 710002;

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var staysDbContext =
                scope.ServiceProvider.GetRequiredService<StaysDbContext>();
            var experiencesDbContext =
                scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>();

            staysDbContext.Stays.AddRange(
                new Stay
                {
                    Name = "Target Stay 100",
                    SourceType = StaySourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    Price = 100
                },
                new Stay
                {
                    Name = "Target Stay 200",
                    SourceType = StaySourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    Price = 200
                },
                new Stay
                {
                    Name = "Other Stay 1000",
                    SourceType = StaySourceType.ThirdParty,
                    Adm2Gid = otherAdm2,
                    Price = 1000
                },
                new Stay
                {
                    Name = "Inactive Stay",
                    SourceType = StaySourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    Price = 9999,
                    IsActive = false
                });

            experiencesDbContext.Experiences.AddRange(
                new Experience
                {
                    Name = "Target Experience 30",
                    Category = ExperienceCategory.Historical,
                    SourceType = ExperienceSourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    PriceRangeMin = 20,
                    PriceRangeMax = 40
                },
                new Experience
                {
                    Name = "Target Experience 50",
                    Category = ExperienceCategory.Nature,
                    SourceType = ExperienceSourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    PriceRangeMin = 50
                },
                new Experience
                {
                    Name = "Other Experience 400",
                    Category = ExperienceCategory.Shopping,
                    SourceType = ExperienceSourceType.ThirdParty,
                    Adm2Gid = otherAdm2,
                    PriceRangeMin = 300,
                    PriceRangeMax = 500
                },
                new Experience
                {
                    Name = "Pending Experience",
                    Category = ExperienceCategory.Dining,
                    SourceType = ExperienceSourceType.ThirdParty,
                    Adm2Gid = targetAdm2,
                    PriceRangeMin = 1,
                    PriceRangeMax = 1,
                    ModerationStatus = ExperienceModerationStatus.Pending
                });

            await staysDbContext.SaveChangesAsync();
            await experiencesDbContext.SaveChangesAsync();
        }

        var response = await Client.GetAsync($"/api/price-index/adm2/{targetAdm2}");
        response.EnsureSuccessStatusCode();

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        Assert.Equal("USD", root.GetProperty("currency").GetString());
        Assert.Equal(
            targetAdm2,
            root.GetProperty("filters").GetProperty("adm2Gid").GetInt32());

        var stays = root.GetProperty("stays");
        Assert.Equal(2, stays.GetProperty("sampleSize").GetInt32());
        Assert.True(stays.GetProperty("indexValue").GetDecimal() > 0);
        Assert.Equal(100m, stays.GetProperty("minimumPrice").GetDecimal());
        Assert.Equal(200m, stays.GetProperty("maximumPrice").GetDecimal());
        Assert.Equal(150m, stays.GetProperty("averagePrice").GetDecimal());

        var experiences = root.GetProperty("experiences");
        Assert.Equal(2, experiences.GetProperty("sampleSize").GetInt32());
        Assert.True(experiences.GetProperty("indexValue").GetDecimal() > 0);
        Assert.Equal(30m, experiences.GetProperty("minimumPrice").GetDecimal());
        Assert.Equal(50m, experiences.GetProperty("maximumPrice").GetDecimal());
        Assert.Equal(40m, experiences.GetProperty("averagePrice").GetDecimal());

        var combined = root.GetProperty("combined");
        Assert.Equal(4, combined.GetProperty("sampleSize").GetInt32());
        Assert.True(combined.GetProperty("indexValue").GetDecimal() > 0);
        Assert.Equal(95m, combined.GetProperty("averagePrice").GetDecimal());
        Assert.Equal(75m, combined.GetProperty("medianPrice").GetDecimal());
        Assert.Equal(45m, combined.GetProperty("percentile25Price").GetDecimal());
        Assert.Equal(125m, combined.GetProperty("percentile75Price").GetDecimal());
    }
}
