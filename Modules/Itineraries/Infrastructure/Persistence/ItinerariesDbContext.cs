using Glinter.Modules.Itineraries.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Itineraries.Infrastructure.Persistence;

public sealed class ItinerariesDbContext(
    DbContextOptions<ItinerariesDbContext> options) : DbContext(options)
{
    public DbSet<SavedItinerary> Itineraries => Set<SavedItinerary>();
    public DbSet<SavedItineraryItem> ItineraryItems => Set<SavedItineraryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("itineraries");
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ItinerariesDbContext).Assembly,
            type => type.Namespace?.StartsWith(
                "Glinter.Modules.Itineraries.Infrastructure.Persistence.Configurations",
                StringComparison.Ordinal) == true);
    }
}
