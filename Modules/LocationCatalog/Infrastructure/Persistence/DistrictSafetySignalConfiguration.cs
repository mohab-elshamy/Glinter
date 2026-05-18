using Glinter.Modules.LocationCatalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.LocationCatalog.Infrastructure.Persistence;

public sealed class DistrictSafetySignalConfiguration : IEntityTypeConfiguration<DistrictSafetySignal>
{
    public void Configure(EntityTypeBuilder<DistrictSafetySignal> builder)
    {
        builder.ToTable("district_safety_signals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.SourceUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.RiskCategory)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Severity)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AiSummary)
            .HasMaxLength(1000);

        builder.Property(x => x.RawAiJson)
            .HasMaxLength(4000);

        builder.Property(x => x.Confidence)
            .IsRequired();

        builder.Property(x => x.SentimentScore)
            .IsRequired();

        builder.Property(x => x.IsSafetyRelevant)
            .IsRequired();

        builder.Property(x => x.PublishedAtUtc)
            .IsRequired();

        builder.Property(x => x.AnalyzedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.DistrictId);

        builder.HasIndex(x => x.PublishedAtUtc);

        builder.HasIndex(x => new
        {
            x.DistrictId,
            x.SourceType,
            x.PublishedAtUtc
        });

        builder.HasOne(x => x.District)
            .WithMany()
            .HasForeignKey(x => x.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}