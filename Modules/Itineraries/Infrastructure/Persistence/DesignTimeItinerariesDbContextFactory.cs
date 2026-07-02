using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Glinter.Modules.Itineraries.Infrastructure.Persistence;

public sealed class DesignTimeItinerariesDbContextFactory
    : IDesignTimeDbContextFactory<ItinerariesDbContext>
{
    public ItinerariesDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found.");
        return new ItinerariesDbContext(
            new DbContextOptionsBuilder<ItinerariesDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}
