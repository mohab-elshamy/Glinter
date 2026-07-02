using Glinter.Modules.Itineraries.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Itineraries.Infrastructure.Persistence.Configurations;

public sealed class SavedItineraryItemConfiguration : IEntityTypeConfiguration<SavedItineraryItem>
{
    public void Configure(EntityTypeBuilder<SavedItineraryItem> builder)
    {
        builder.ToTable("saved_itinerary_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(40);
        builder.Property(x => x.NameSnapshot).IsRequired().HasMaxLength(240);
        builder.Property(x => x.Explanation).HasMaxLength(1500);
        builder.Property(x => x.TravelModeFromPrevious).HasMaxLength(40);
        builder.Property(x => x.EstimatedCost).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.ItineraryId, x.DayNumber, x.SortOrder })
            .IsUnique();
    }
}
