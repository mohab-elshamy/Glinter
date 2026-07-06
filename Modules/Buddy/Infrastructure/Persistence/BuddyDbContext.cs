using Glinter.Modules.Buddy.Application.Abstractions;
using Glinter.Modules.Buddy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Buddy.Infrastructure.Persistence;

public sealed class BuddyDbContext : DbContext, IBuddyDbContext
{
    public BuddyDbContext(DbContextOptions<BuddyDbContext> options)
        : base(options)
    {
    }

    public DbSet<BuddyAvailability> BuddyAvailabilities =>
        Set<BuddyAvailability>();

    public DbSet<BuddyBooking> BuddyBookings => Set<BuddyBooking>();

    public DbSet<BuddyReview> BuddyReviews => Set<BuddyReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BuddyDbContext).Assembly,
            type => type.Namespace?.StartsWith(
                "Glinter.Modules.Buddy.Infrastructure.Persistence.Configurations",
                StringComparison.Ordinal) == true);

        // Profiles migrations created these physical tables. Excluding them here
        // lets Buddy own runtime persistence without a destructive second history.
        modelBuilder.Entity<BuddyAvailability>()
            .ToTable("buddy_availability", table => table.ExcludeFromMigrations());
        modelBuilder.Entity<BuddyBooking>()
            .ToTable("buddy_bookings", table => table.ExcludeFromMigrations());
        modelBuilder.Entity<BuddyReview>()
            .ToTable("buddy_reviews", table => table.ExcludeFromMigrations());
    }
}
