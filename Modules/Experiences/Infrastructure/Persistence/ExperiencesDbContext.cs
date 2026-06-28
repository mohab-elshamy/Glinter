using Glinter.Modules.Experiences.Application.Abstractions;
using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence;

public class ExperiencesDbContext : DbContext, IExperiencesDbContext
{
    public ExperiencesDbContext(DbContextOptions<ExperiencesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperienceCategory> ExperienceCategories => Set<ExperienceCategory>();
    public DbSet<Vibe> Vibes => Set<Vibe>();
    public DbSet<ExperienceAvailability> ExperienceAvailability => Set<ExperienceAvailability>();
    public DbSet<ExperienceBooking> ExperienceBookings => Set<ExperienceBooking>();
    public DbSet<ExperienceReview> ExperienceReviews => Set<ExperienceReview>();
    public DbSet<ExperienceVibe> ExperienceVibes => Set<ExperienceVibe>();
    public DbSet<ExperienceTag> ExperienceTags => Set<ExperienceTag>();
    public DbSet<ExperienceModerationEvent> ExperienceModerationEvents =>
        Set<ExperienceModerationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ExperiencesDbContext).Assembly,
            type => type.Namespace != null &&
                    type.Namespace.StartsWith("Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations"));
    }
}
