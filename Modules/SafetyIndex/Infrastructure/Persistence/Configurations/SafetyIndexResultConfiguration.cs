using Glinter.Modules.SafetyIndex.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.SafetyIndex.Infrastructure.Persistence.Configurations;

public class SafetyIndexResultConfiguration : IEntityTypeConfiguration<SafetyIndexResult>
{
    public void Configure(EntityTypeBuilder<SafetyIndexResult> builder)
    {
        builder.ToTable("safety_index_results");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Adm2Gid)
            .IsUnique();

        builder.Property(x => x.WeeklyGeneralSafetyDescription)
            .HasMaxLength(2000);

        builder.Property(x => x.WeeklyTrendingEventDescription)
            .HasMaxLength(2000);

        builder.Property(x => x.HistoricalGeneralSafetyDescription)
            .HasMaxLength(2000);

        builder.Property(x => x.HistoricalTrendingEventDescription)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();
    }
}
