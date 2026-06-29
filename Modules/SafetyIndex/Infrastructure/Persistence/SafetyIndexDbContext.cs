using Glinter.Modules.SafetyIndex.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.SafetyIndex.Infrastructure.Persistence;

public class SafetyIndexDbContext : DbContext
{
    public SafetyIndexDbContext(DbContextOptions<SafetyIndexDbContext> options) : base(options)
    {
    }

    public DbSet<SafetyIndexResult> SafetyIndexResults => Set<SafetyIndexResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SafetyIndexDbContext).Assembly,
            type => type.Namespace != null &&
                    type.Namespace.StartsWith("Glinter.Modules.SafetyIndex.Infrastructure.Persistence.Configurations"));
    }
}
