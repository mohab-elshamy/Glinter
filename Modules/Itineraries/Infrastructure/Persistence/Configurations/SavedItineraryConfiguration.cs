using Glinter.Modules.Itineraries.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Itineraries.Infrastructure.Persistence.Configurations;

public sealed class SavedItineraryConfiguration : IEntityTypeConfiguration<SavedItinerary>
{
    public void Configure(EntityTypeBuilder<SavedItinerary> builder)
    {
        builder.ToTable("saved_itineraries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Destination).HasMaxLength(240);
        builder.Property(x => x.PreferredLanguage).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Currency).HasMaxLength(8);
        builder.Property(x => x.EstimatedTotalCost).HasPrecision(18, 2);
        builder.Property(x => x.PlannerExplanation).HasMaxLength(4000);
        builder.Property(x => x.WarningsJson).HasColumnType("jsonb");
        builder.Property(x => x.Pace).HasMaxLength(40);
        builder.Property(x => x.TravelMode).HasMaxLength(40);
        builder.Property(x => x.FallbackTravelMode).HasMaxLength(40);
        builder.Property(x => x.OriginLabel).HasMaxLength(240);
        builder.Property(x => x.WeatherLocation).HasMaxLength(240);
        builder.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        builder.HasIndex(x => new { x.UserId, x.UpdatedAtUtc });
        builder.HasMany(x => x.Items)
            .WithOne(x => x.Itinerary)
            .HasForeignKey(x => x.ItineraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
