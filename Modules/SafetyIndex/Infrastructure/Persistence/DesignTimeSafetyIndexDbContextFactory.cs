using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Glinter.Modules.SafetyIndex.Infrastructure.Persistence;

public class DesignTimeSafetyIndexDbContextFactory : IDesignTimeDbContextFactory<SafetyIndexDbContext>
{
    public SafetyIndexDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<SafetyIndexDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new SafetyIndexDbContext(optionsBuilder.Options);
    }
}
