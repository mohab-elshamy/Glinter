using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Glinter.Shared.Infrastructure.Health;

public sealed class PostgresReadinessHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PostgresReadinessHealthCheck(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var contexts = new DbContext[]
            {
                scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>(),
                scope.ServiceProvider.GetRequiredService<ProfilesDbContext>(),
                scope.ServiceProvider.GetRequiredService<RegionsDbContext>(),
                scope.ServiceProvider.GetRequiredService<StaysDbContext>(),
                scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>(),
                scope.ServiceProvider.GetRequiredService<CommunicationDbContext>()
            };

            var pendingContexts = new List<string>();
            foreach (var dbContext in contexts)
            {
                var pendingMigrations = await dbContext.Database
                    .GetPendingMigrationsAsync(cancellationToken);
                if (pendingMigrations.Any())
                    pendingContexts.Add(dbContext.GetType().Name);
            }

            if (pendingContexts.Count > 0)
            {
                return HealthCheckResult.Unhealthy(
                    "Database schema migrations are pending.",
                    data: new Dictionary<string, object>
                    {
                        ["pendingContexts"] = pendingContexts
                    });
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.", exception);
        }
    }
}
