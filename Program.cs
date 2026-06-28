using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Glinter.Modules.Communication.Infrastructure.DependencyInjection;
using Glinter.Modules.Experiences.Infrastructure.DependencyInjection;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Infrastructure.Identity;
using Glinter.Modules.Profiles.Infrastructure.DependencyInjection;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.DependencyInjection;
using Glinter.Modules.Stays.Infrastructure.DependencyInjection;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Glinter.Shared.Infrastructure.Errors;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Glinter.Modules.Communication.Infrastructure.Realtime;
using Glinter.Shared.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Glinter.Shared.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Glinter.Shared.Infrastructure.Dashboards;
using Glinter.Shared.Infrastructure.Analytics;
using Glinter.Shared.Infrastructure.Metrics;
using Glinter.Shared.Infrastructure.Cleanup;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId;
});
// Hosting.Diagnostics logs the raw request URL before middleware can redact
// SignalR's access_token query parameter. The safe request logger below
// replaces those start/finish records.
builder.Logging.AddFilter(
    "Microsoft.AspNetCore.Hosting.Diagnostics",
    LogLevel.Warning);
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.SingleLine = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
        options.UseUtcTimestamp = true;
    });
}
else
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        options.UseUtcTimestamp = true;
    });
}

const string CorsPolicyName = "GlinterFrontend";

var maxRequestBodyBytes = builder.Configuration.GetValue<long?>("RequestLimits:MaxBodyBytes")
                          ?? 50L * 1024 * 1024;

if (maxRequestBodyBytes <= 0)
    throw new InvalidOperationException("RequestLimits:MaxBodyBytes must be greater than zero.");

var rateLimitPermitLimit =
    builder.Configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 120;
var rateLimitWindowMinutes =
    builder.Configuration.GetValue<int?>("RateLimiting:WindowMinutes") ?? 1;

if (rateLimitPermitLimit <= 0)
    throw new InvalidOperationException("RateLimiting:PermitLimit must be greater than zero.");

if (rateLimitWindowMinutes <= 0)
    throw new InvalidOperationException("RateLimiting:WindowMinutes must be greater than zero.");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBodyBytes;
});

// Module 1: IdentityAccess
builder.Services.AddIdentityAccessModule(builder.Configuration, builder.Environment);

// Module 2: Profiles
builder.Services.AddProfilesModule(builder.Configuration);

// Module 4: Stays
builder.Services.AddDbContext<StaysDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStaysModule();

// Module 5: Experiences
builder.Services.AddExperiencesModule(builder.Configuration);

// Module 6: Regions (Administrative Boundaries)
builder.Services.AddRegionsModule(builder.Configuration);

// Module 9: Communication
builder.Services.AddCommunicationModule(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 16 * 1024;
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<PostgresReadinessHealthCheck>("postgres", tags: ["ready"]);
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AdminAnalyticsService>();
builder.Services.AddSingleton<ApplicationMetrics>();

var cleanupOptions = builder.Configuration
    .GetSection(DataCleanupOptions.SectionName)
    .Get<DataCleanupOptions>() ?? new DataCleanupOptions();
if (cleanupOptions.IntervalMinutes <= 0 ||
    cleanupOptions.RevokedTokenRetentionDays < 0 ||
    cleanupOptions.RefreshTokenRetentionDays < 0 ||
    cleanupOptions.MfaChallengeRetentionDays < 0 ||
    cleanupOptions.ReadNotificationRetentionDays < 1 ||
    cleanupOptions.UnreadNotificationRetentionDays <
    cleanupOptions.ReadNotificationRetentionDays)
{
    throw new InvalidOperationException(
        "Cleanup configuration is invalid; interval must be positive and unread notification retention must be at least the read retention.");
}
builder.Services.Configure<DataCleanupOptions>(
    builder.Configuration.GetSection(DataCleanupOptions.SectionName));
builder.Services.AddScoped<DataCleanupService>();
if (cleanupOptions.Enabled)
    builder.Services.AddHostedService<DataCleanupWorker>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problemDetails = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed.",
            Detail = "One or more request values are invalid.",
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = "validation_error";
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        var result = new BadRequestObjectResult(problemDetails);
        result.ContentTypes.Add("application/problem+json");
        return result;
    };
});

var configuredAllowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

var allowedOrigins = configuredAllowedOrigins is { Length: > 0 }
    ? configuredAllowedOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray()
    : builder.Environment.IsDevelopment()
        ?
        [
            "http://localhost:3000",
            "https://localhost:3000",
            "http://localhost:5173",
            "https://localhost:5173"
        ]
        : [];

if (allowedOrigins.Any(origin => origin == "*"))
    throw new InvalidOperationException("Cors:AllowedOrigins cannot contain a wildcard origin.");

if (!builder.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "Cors:AllowedOrigins must contain at least one explicit origin outside Development.");

foreach (var origin in allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
        uri.AbsolutePath != "/" ||
        !string.IsNullOrEmpty(uri.Query) ||
        !string.IsNullOrEmpty(uri.Fragment))
    {
        throw new InvalidOperationException(
            $"Cors:AllowedOrigins contains an invalid origin: '{origin}'.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Detail = "Too many requests were sent in a short period. Try again later.",
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = "rate_limit_exceeded";
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitPermitLimit,
                Window = TimeSpan.FromMinutes(rateLimitWindowMinutes),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    });
});

#region Swagger Configurations
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Glinter API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});
#endregion

var app = builder.Build();

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Glinter API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}
else
{
    app.MapGet("/", () => Results.Ok(new
    {
        name = "Glinter API",
        status = "OK"
    }));
}

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat", options =>
{
    options.CloseOnAuthenticationExpiration = true;
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live")
}).DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).DisableRateLimiting();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var adminSeedOptions = new AdminSeedOptions
    {
        Email = builder.Configuration["AdminSeed:Email"] ?? string.Empty,
        Password = builder.Configuration["AdminSeed:Password"] ?? string.Empty,
        FullName = builder.Configuration["AdminSeed:FullName"] ?? "System Admin"
    };

    await IdentitySeeder.SeedAsync(roleManager, userManager, adminSeedOptions);
}

using (var scope = app.Services.CreateScope())
{
    var profilesDbContext = scope.ServiceProvider.GetRequiredService<ProfilesDbContext>();
    await ProfilesSeeder.SeedAsync(profilesDbContext);
}

using (var scope = app.Services.CreateScope())
{
    var experiencesDbContext = scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>();
    await ExperiencesSeeder.SeedAsync(experiencesDbContext);
}

app.Run();

public partial class Program;
