using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Persistence;

public class DesignTimeIdentityAccessDbContextFactory 
    : IDesignTimeDbContextFactory<IdentityAccessDbContext>
{
    public IdentityAccessDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IdentityAccessDbContext>();

        var connectionString =
            "Host=localhost;Port=5432;Database=glinter_db;Username=postgres;Password=3oza@2004";

        optionsBuilder.UseNpgsql(connectionString);

        return new IdentityAccessDbContext(optionsBuilder.Options);
    }
}