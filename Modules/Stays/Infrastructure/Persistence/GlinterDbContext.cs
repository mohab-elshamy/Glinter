using Glinter.Modules.Stays.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Stays.Infrastructure.Persistence;

public class GlinterDbContext : DbContext
{
    public GlinterDbContext(DbContextOptions<GlinterDbContext> options) : base(options)
    {
    }

    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<StayBooking> StayBookings => Set<StayBooking>();
    public DbSet<StayReview> StayReviews => Set<StayReview>();
    public DbSet<StayTag> StayTags => Set<StayTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GlinterDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}