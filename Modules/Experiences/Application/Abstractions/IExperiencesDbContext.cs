using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Application.Abstractions;

public interface IExperiencesDbContext
{
    DbSet<Experience> Experiences { get; }
    DbSet<ExperienceCategory> ExperienceCategories { get; }
    DbSet<Vibe> Vibes { get; }
    DbSet<ExperienceAvailability> ExperienceAvailability { get; }
    DbSet<ExperienceBooking> ExperienceBookings { get; }
    DbSet<ExperienceReview> ExperienceReviews { get; }
    DbSet<ExperienceVibe> ExperienceVibes { get; }
    DbSet<ExperienceTag> ExperienceTags { get; }
    DbSet<ExperienceModerationEvent> ExperienceModerationEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
