using Glinter.Modules.Regions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Configurations;

public class Adm0Configuration : IEntityTypeConfiguration<Adm0>
{
    public void Configure(EntityTypeBuilder<Adm0> builder)
    {
        builder.ToTable("adm0");

        builder.HasKey(x => x.Gid);

        builder.Property(x => x.Gid)
            .HasColumnName("gid")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.NameEn)
            .HasColumnName("name_en")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NameAr)
            .HasColumnName("name_ar")
            .HasMaxLength(100);

        builder.Property(x => x.Pcode)
            .HasColumnName("pcode")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.BoundaryGeom)
            .HasColumnName("boundary_geom")
            .HasColumnType("geometry(MultiPolygon, 4326)");

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url");

        builder.Property(x => x.FlagUrl)
            .HasColumnName("flag_url");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.Pcode)
            .IsUnique()
            .HasDatabaseName("ix_adm0_pcode");

        builder.HasIndex(x => x.BoundaryGeom)
            .HasMethod("gist")
            .HasDatabaseName("ix_adm0_boundary_geom");

        // Navigation
        builder.HasMany(x => x.Governorates)
            .WithOne(x => x.Country)
            .HasForeignKey(x => x.Adm0Gid)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
