using System.Text;
using System.Security.Cryptography;
using Glinter.Modules.Communication.Infrastructure.Persistence;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Security;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.Persistence;
using Glinter.Modules.SafetyIndex.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Glinter.IntegrationTests.Infrastructure;

public sealed class GlinterApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string JwtIssuer = "Glinter.IntegrationTests";
    private const string JwtAudience = "Glinter.IntegrationTests";
    private const string JwtSecret =
        "integration-tests-secret-key-with-at-least-32-bytes";

    public const string AdminEmail = "integration-admin@glinter.test";
    public const string AdminPassword = "IntegrationAdmin!2026";
    public const string UserPassword = "IntegrationUser!2026";

    private string _databaseName = string.Empty;
    private string _adminConnectionString = string.Empty;

    public string ConnectionString { get; private set; } = string.Empty;
    public string? AdminMfaSharedKey { get; set; }
    public TestLogCollector LogCollector { get; } = new();

    public async Task InitializeAsync()
    {
        var configuredConnection = Environment.GetEnvironmentVariable(
            "GLINTER_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>(optional: true)
                .Build();
            configuredConnection = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            throw new InvalidOperationException(
                "Set GLINTER_TEST_CONNECTION_STRING or configure the application's DefaultConnection user secret.");
        }

        _databaseName = $"glinter_integration_{Guid.NewGuid():N}";

        var testBuilder = new NpgsqlConnectionStringBuilder(configuredConnection)
        {
            Database = _databaseName,
            Pooling = false,
            Timeout = 15,
            CommandTimeout = 30
        };
        ConnectionString = testBuilder.ConnectionString;

        var adminBuilder = new NpgsqlConnectionStringBuilder(configuredConnection)
        {
            Database = "postgres",
            Pooling = false,
            Timeout = 15,
            CommandTimeout = 30
        };
        _adminConnectionString = adminBuilder.ConnectionString;

        await using (var connection = new NpgsqlConnection(_adminConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE EXTENSION IF NOT EXISTS pg_trgm";
            await command.ExecuteNonQueryAsync();
        }

        await ApplyMigrationsAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.AddProvider(LogCollector));
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["AdminSeed:Email"] = AdminEmail,
                ["AdminSeed:Password"] = AdminPassword,
                ["AdminSeed:FullName"] = "Integration Admin",
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:SecretKey"] = JwtSecret,
                ["Jwt:ExpiryMinutes"] = "60",
                ["IdentityEmail:SmtpHost"] = string.Empty,
                ["HotelRecommendations:GroqApiKey"] = string.Empty,
                ["Cleanup:Enabled"] = "false",
                ["SafetyIndex:EnableWeeklyService"] = "false",
                ["SafetyIndex:RunInitialHistoricalCollectionOnStartup"] = "false",
                ["RateLimiting:PermitLimit"] = "10000",
                ["RateLimiting:WindowMinutes"] = "1",
                ["Communication:RateLimiting:DirectThreadPermitLimit"] = "5",
                ["Communication:RateLimiting:MessagePermitLimit"] = "8",
                ["Communication:RateLimiting:WindowSeconds"] = "60"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            ReplaceDbContext<IdentityAccessDbContext>(services, false);
            ReplaceDbContext<ProfilesDbContext>(services, false);
            ReplaceDbContext<RegionsDbContext>(services, true);
            ReplaceDbContext<SafetyIndexDbContext>(services, false);
            ReplaceDbContext<StaysDbContext>(services, false);
            ReplaceDbContext<ExperiencesDbContext>(services, false);
            ReplaceDbContext<CommunicationDbContext>(services, false);

            services.PostConfigure<JwtOptions>(options =>
            {
                options.Issuer = JwtIssuer;
                options.Audience = JwtAudience;
                options.SecretKey = JwtSecret;
                options.ExpiryMinutes = 60;
            });
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.ValidIssuer = JwtIssuer;
                    options.TokenValidationParameters.ValidAudience = JwtAudience;
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
                });
            services.RemoveAll<IPasswordHasher<ApplicationUser>>();
            services.AddSingleton<IPasswordHasher<ApplicationUser>, FastTestPasswordHasher>();
        });
    }

    public async Task<int> ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)result;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        NpgsqlConnection.ClearAllPools();

        if (string.IsNullOrWhiteSpace(_adminConnectionString) ||
            string.IsNullOrWhiteSpace(_databaseName))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using (var terminate = connection.CreateCommand())
        {
            terminate.CommandText =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName AND pid <> pg_backend_pid()
                """;
            terminate.Parameters.AddWithValue("databaseName", _databaseName);
            await terminate.ExecuteNonQueryAsync();
        }

        await using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
        await drop.ExecuteNonQueryAsync();
    }

    private async Task ApplyMigrationsAsync()
    {
        var identityOptions = new DbContextOptionsBuilder<IdentityAccessDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new IdentityAccessDbContext(identityOptions))
            await context.Database.MigrateAsync();

        var profileOptions = new DbContextOptionsBuilder<ProfilesDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new ProfilesDbContext(profileOptions))
            await context.Database.MigrateAsync();

        var regionOptions = new DbContextOptionsBuilder<RegionsDbContext>()
            .UseNpgsql(ConnectionString, options => options.UseNetTopologySuite())
            .Options;
        await using (var context = new RegionsDbContext(regionOptions))
            await context.Database.MigrateAsync();

        var safetyOptions = new DbContextOptionsBuilder<SafetyIndexDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new SafetyIndexDbContext(safetyOptions))
            await context.Database.MigrateAsync();

        var stayOptions = new DbContextOptionsBuilder<StaysDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new StaysDbContext(stayOptions))
            await context.Database.MigrateAsync();

        var experienceOptions = new DbContextOptionsBuilder<ExperiencesDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new ExperiencesDbContext(experienceOptions))
            await context.Database.MigrateAsync();

        var communicationOptions = new DbContextOptionsBuilder<CommunicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using (var context = new CommunicationDbContext(communicationOptions))
            await context.Database.MigrateAsync();
    }

    private void ReplaceDbContext<TContext>(
        IServiceCollection services,
        bool useNetTopologySuite)
        where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<DbContextOptions<TContext>>();
        services.AddDbContext<TContext>(options =>
        {
            if (useNetTopologySuite)
            {
                options.UseNpgsql(
                    ConnectionString,
                    npgsql => npgsql.UseNetTopologySuite());
            }
            else
            {
                options.UseNpgsql(ConnectionString);
            }
        });
    }

    private sealed class FastTestPasswordHasher
        : IPasswordHasher<ApplicationUser>
    {
        private const string Prefix = "integration-sha256:";

        public string HashPassword(ApplicationUser user, string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            return Prefix + Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        }

        public PasswordVerificationResult VerifyHashedPassword(
            ApplicationUser user,
            string hashedPassword,
            string providedPassword)
        {
            if (!hashedPassword.StartsWith(Prefix, StringComparison.Ordinal))
                return PasswordVerificationResult.Failed;

            var expected = HashPassword(user, providedPassword);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hashedPassword),
                Encoding.UTF8.GetBytes(expected))
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}
