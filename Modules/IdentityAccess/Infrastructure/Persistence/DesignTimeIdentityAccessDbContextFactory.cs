using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Persistence;

public class DesignTimeIdentityAccessDbContextFactory
    : IDesignTimeDbContextFactory<IdentityAccessDbContext>
{
    public IdentityAccessDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection not found.");

        var optionsBuilder = new DbContextOptionsBuilder<IdentityAccessDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new IdentityAccessDbContext(optionsBuilder.Options);
    }
}