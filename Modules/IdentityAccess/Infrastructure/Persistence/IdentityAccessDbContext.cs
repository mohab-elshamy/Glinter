using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Glinter.Shared.Application.Auditing;

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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();
    public DbSet<AdminAuditEvent> AdminAuditEvents => Set<AdminAuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(x => x.FullName).HasMaxLength(200);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => x.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique();
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.IsActive);
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
            entity.HasIndex(x => x.ExpiresAtUtc);

            entity.Property(x => x.RevokedAtUtc)
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            entity.Property(x => x.Reason)
                .HasMaxLength(500);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RevocationReason).HasMaxLength(200);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.FamilyId });
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasIndex(x => x.RevokedAtUtc);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MfaChallenge>(entity =>
        {
            entity.ToTable("mfa_challenges");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Purpose).HasMaxLength(20).IsRequired();
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasIndex(x => x.ConsumedAtUtc);
            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AdminAuditEvent>(entity =>
        {
            entity.ToTable("admin_audit_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(200).IsRequired();
            entity.Property(x => x.HttpMethod).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Target).HasMaxLength(1000);
            entity.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ChangeDetailsJson).HasMaxLength(16000);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => new { x.ActorUserId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.Action, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.CreatedAtUtc, x.CompletedAtUtc })
                .HasFilter("\"CompletedAtUtc\" IS NOT NULL");
        });
    }
}
