using Glinter.Modules.Profiles.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Profiles.Infrastructure.Persistence;

public static class ProfilesSeeder
{
    public static async Task SeedAsync(ProfilesDbContext dbContext)
    {
        var defaultInterests = new[]
        {
            "Food",
            "Museums",
            "History",
            "Adventure",
            "Shopping",
            "Nature",
            "Photography",
            "Nightlife",
            "Culture",
            "Beaches"
        };

        foreach (var interestName in defaultInterests)
        {
            var exists = await dbContext.Interests
                .AnyAsync(x => x.Name == interestName);

            if (!exists)
            {
                dbContext.Interests.Add(new Interest
                {
                    Id = Guid.NewGuid(),
                    Name = interestName,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }
}