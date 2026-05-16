using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Persistence;

public class StaysDbContext : DbContext
{
    public StaysDbContext(DbContextOptions<StaysDbContext> options) : base(options)
    {
    }

    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<StayBooking> StayBookings => Set<StayBooking>();
    public DbSet<StayReview> StayReviews => Set<StayReview>();
    public DbSet<StayTag> StayTags => Set<StayTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(StaysDbContext).Assembly,
        type => type.Namespace != null &&
                type.Namespace.StartsWith("Glinter.Modules.Stays.Infrastructure.Persistence.Configurations"));
}
}