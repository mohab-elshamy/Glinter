using Glinter.Modules.Regions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Regions.Infrastructure.Persistence;

public class RegionsDbContext : DbContext
{
    public RegionsDbContext(DbContextOptions<RegionsDbContext> options) : base(options)
    {
    }

    public DbSet<Adm0> Adm0 => Set<Adm0>();
    public DbSet<Adm1> Adm1 => Set<Adm1>();
    public DbSet<Adm2> Adm2 => Set<Adm2>();
    public DbSet<Adm3> Adm3 => Set<Adm3>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(RegionsDbContext).Assembly,
            type => type.Namespace != null &&
                    type.Namespace.StartsWith("Glinter.Modules.Regions.Infrastructure.Persistence.Configurations"));
    }
}
