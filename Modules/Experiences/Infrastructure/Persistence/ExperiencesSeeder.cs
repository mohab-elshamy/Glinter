using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence;

public static class ExperiencesSeeder
{
    private static readonly (string Name, string Description)[] DefaultCategories =
    {
        ("Food Tour", "Food-based local experiences and tasting tours."),
        ("Historical Walk", "Guided walks around historical and heritage areas."),
        ("Museum Visit", "Museum and cultural site experiences."),
        ("Cultural Workshop", "Hands-on cultural and local craft workshops."),
        ("Adventure", "Outdoor, active, and adventure experiences."),
        ("Photography", "Photography walks and visual storytelling experiences."),
        ("Shopping", "Local markets, bazaars, and shopping experiences."),
        ("Nightlife", "Evening and nightlife experiences.")
    };

    private static readonly string[] DefaultVibes =
    {
        "Cultural",
        "Historical",
        "Foodie",
        "Adventure",
        "Relaxed",
        "Hidden Gems",
        "Budget Friendly",
        "Luxury",
        "Family Friendly",
        "Romantic"
    };

    public static async Task SeedAsync(
        ExperiencesDbContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var category in DefaultCategories)
        {
            var exists = await context.ExperienceCategories.AnyAsync(
                x => x.Name == category.Name,
                cancellationToken);

            if (!exists)
            {
                await context.ExperienceCategories.AddAsync(
                    new ExperienceCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = category.Name,
                        Description = category.Description,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken);
            }
        }

        foreach (var vibe in DefaultVibes)
        {
            var exists = await context.Vibes.AnyAsync(
                x => x.Name == vibe,
                cancellationToken);

            if (!exists)
            {
                await context.Vibes.AddAsync(
                    new Vibe
                    {
                        Id = Guid.NewGuid(),
                        Name = vibe,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}