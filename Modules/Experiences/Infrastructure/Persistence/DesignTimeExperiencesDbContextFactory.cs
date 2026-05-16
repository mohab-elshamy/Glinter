using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Glinter.Modules.Experiences.Infrastructure.Persistence;

public class DesignTimeExperiencesDbContextFactory : IDesignTimeDbContextFactory<ExperiencesDbContext>
{
    public ExperiencesDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<ExperiencesDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ExperiencesDbContext(optionsBuilder.Options);
    }
}