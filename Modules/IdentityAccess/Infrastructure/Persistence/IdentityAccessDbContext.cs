using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Persistence;

public class IdentityAccessDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
      IIdentityAccessDbContext
{
    public IdentityAccessDbContext(DbContextOptions<IdentityAccessDbContext> options)
        : base(options)
    {
    }

    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("roles");
        });

        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.Entity<RevokedToken>(entity =>
        {
            entity.ToTable("revoked_tokens");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Jti)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(x => x.Jti)
                .IsUnique();

            entity.Property(x => x.RevokedAtUtc)
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            entity.Property(x => x.Reason)
                .HasMaxLength(500);
        });
    }
}