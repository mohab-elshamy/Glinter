using Glinter.Modules.Experiences.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence;

public class ExperiencesDbContext : DbContext
{
    public ExperiencesDbContext(DbContextOptions<ExperiencesDbContext> options) : base(options)
    {
    }

    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperienceFeaturedImage> ExperienceFeaturedImages => Set<ExperienceFeaturedImage>();
    public DbSet<ExperienceHour> ExperienceHours => Set<ExperienceHour>();
    public DbSet<ExperiencePopularTime> ExperiencePopularTimes => Set<ExperiencePopularTime>();
    public DbSet<ExperienceReviewsPerRating> ExperienceReviewsPerRatings => Set<ExperienceReviewsPerRating>();
    public DbSet<ExperienceAmenity> ExperienceAmenities => Set<ExperienceAmenity>();
    public DbSet<ExperienceReview> ExperienceReviews => Set<ExperienceReview>();
    public DbSet<ExperienceAvailability> ExperienceAvailabilitySlots => Set<ExperienceAvailability>();
    public DbSet<ExperienceBooking> ExperienceBookings => Set<ExperienceBooking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("experiences");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ExperiencesDbContext).Assembly,
            type => type.Namespace != null &&
                    type.Namespace.StartsWith("Glinter.Modules.Experiences.Infrastructure.Persistence.Configurations"));
    }
}
