using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Persistence;

public class StaysDbContext : DbContext
{
    public StaysDbContext(DbContextOptions<StaysDbContext> options) : base(options)
    {
    }

    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<StayImage> StayImages => Set<StayImage>();
    public DbSet<StayAmenity> StayAmenities => Set<StayAmenity>();
    public DbSet<StayReviewsPerRating> StayReviewsPerRatings => Set<StayReviewsPerRating>();
    public DbSet<StayBookingPlatform> StayBookingPlatforms => Set<StayBookingPlatform>();
    public DbSet<StayReview> StayReviews => Set<StayReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("stays");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(StaysDbContext).Assembly,
            type => type.Namespace != null &&
                    type.Namespace.StartsWith("Glinter.Modules.Stays.Infrastructure.Persistence.Configurations"));
    }
}
