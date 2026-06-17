using Glinter.Modules.Regions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Glinter.Modules.Regions.Infrastructure.Persistence.Configurations;

public class Adm3Configuration : IEntityTypeConfiguration<Adm3>
{
    public void Configure(EntityTypeBuilder<Adm3> builder)
    {
        builder.ToTable("adm3");

        builder.HasKey(x => x.Gid);

        builder.Property(x => x.Gid)
            .HasColumnName("gid")
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Adm2Gid)
            .HasColumnName("adm2_gid")
            .IsRequired();

        builder.Property(x => x.NameEn)
            .HasColumnName("name_en")
            .IsRequired(false)
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

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(x => x.Pcode)
            .IsUnique()
            .HasDatabaseName("ix_adm3_pcode");

        builder.HasIndex(x => x.BoundaryGeom)
            .HasMethod("gist")
            .HasDatabaseName("ix_adm3_boundary_geom");
    }
}
