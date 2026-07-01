using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Glinter.Modules.SafetyIndex.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Glinter.Shared.Infrastructure.Health;

public sealed class PostgresReadinessHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PostgresReadinessHealthCheck> _logger;

    public PostgresReadinessHealthCheck(
        IServiceScopeFactory scopeFactory,
        ILogger<PostgresReadinessHealthCheck> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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
                scope.ServiceProvider
                    .GetRequiredService<IdentityAccessDbContext>(),

                scope.ServiceProvider
                    .GetRequiredService<ProfilesDbContext>(),

                scope.ServiceProvider
                    .GetRequiredService<RegionsDbContext>(),

                scope.ServiceProvider
                    .GetRequiredService<StaysDbContext>(),

                scope.ServiceProvider
                    .GetRequiredService<ExperiencesDbContext>(),

                scope.ServiceProvider
                    .GetRequiredService<SafetyIndexDbContext>(),

                scope.ServiceProvider
                    .GetRequiredService<CommunicationDbContext>()
            };

            var pendingMigrationsByContext =
                new Dictionary<string, string[]>();

            foreach (var dbContext in contexts)
            {
                var contextName = dbContext.GetType().Name;

                var pendingMigrations = (
                    await dbContext.Database.GetPendingMigrationsAsync(
                        cancellationToken))
                    .ToArray();

                if (pendingMigrations.Length == 0)
                {
                    continue;
                }

                pendingMigrationsByContext[contextName] =
                    pendingMigrations;
            }

            if (pendingMigrationsByContext.Count > 0)
            {
                var pendingContextNames =
                    pendingMigrationsByContext.Keys.ToArray();

                _logger.LogWarning(
                    "Readiness check failed because migrations are pending for: {PendingContexts}",
                    string.Join(", ", pendingContextNames));

                return HealthCheckResult.Unhealthy(
                    description:
                        "Database schema migrations are pending.",
                    data: new Dictionary<string, object>
                    {
                        ["pendingContexts"] =
                            pendingContextNames,

                        ["pendingMigrations"] =
                            pendingMigrationsByContext
                    });
            }

            return HealthCheckResult.Healthy(
                "PostgreSQL is available and all module migrations are applied.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "PostgreSQL readiness check failed.");

            return HealthCheckResult.Unhealthy(
                description:
                    "PostgreSQL is unavailable or could not be checked.",
                exception: exception);
        }
    }
}