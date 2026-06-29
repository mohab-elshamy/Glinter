using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence;

public class DesignTimeExperiencesDbContextFactory : IDesignTimeDbContextFactory<ExperiencesDbContext>
{
    public ExperiencesDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ExperiencesDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ExperiencesDbContext(optionsBuilder.Options);
    }
}
