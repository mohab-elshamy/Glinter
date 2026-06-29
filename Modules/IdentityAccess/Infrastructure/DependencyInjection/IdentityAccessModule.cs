using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Login;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Logout;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetCurrentUser;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Infrastructure.Security;
using Glinter.Modules.IdentityAccess.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.AssignRole;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.ChangeUserStatus;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUserById;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUsers;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetRoles;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.SecurityFlows;
using Glinter.Modules.IdentityAccess.Infrastructure.Email;



namespace Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;

public static class IdentityAccessModule
{
    public static IServiceCollection AddIdentityAccessModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection not found.");

        services.AddDbContext<IdentityAccessDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IIdentityAccessDbContext>(sp =>
            sp.GetRequiredService<IdentityAccessDbContext>());

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<IdentityAccessDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException("Jwt configuration not found.");

        ValidateJwtOptions(jwtOptions);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                ClockSkew = TimeSpan.FromMinutes(1),
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrWhiteSpace(accessToken) &&
                        context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                    if (string.IsNullOrWhiteSpace(jti))
                    {
                        context.Fail("Token does not contain jti.");
                        return;
                    }

                    var userIdValue =
                        context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                    if (!Guid.TryParse(userIdValue, out var userId))
                    {
                        context.Fail("Token does not contain a valid user id.");
                        return;
                    }

                    var userReadService = context.HttpContext.RequestServices
                        .GetRequiredService<IIdentityUserReadService>();

                    var securityStamp =
                        context.Principal?.FindFirstValue(ClaimNames.SecurityStamp);
                    var isActiveUser = await userReadService.IsActiveUserWithSecurityStampAsync(
                        userId,
                        securityStamp ?? string.Empty,
                        context.HttpContext.RequestAborted);
                    if (!isActiveUser)
                    {
                        context.Fail("User is inactive.");
                        return;
                    }

                    var revocationService = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenRevocationService>();

                    var revoked = await revocationService.IsRevokedAsync(
                        jti,
                        context.HttpContext.RequestAborted);
                    if (revoked)
                    {
                        context.Fail("Token has been revoked.");
                    }
                },
                OnChallenge = async context =>
                {
                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    context.HandleResponse();

                    await WriteAuthenticationProblemDetailsAsync(
                        context.HttpContext,
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized.",
                        "Invalid or expired access token.",
                        "authentication_required");
                },
                OnForbidden = async context =>
                {
                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    await WriteAuthenticationProblemDetailsAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "Forbidden.",
                        "You do not have permission to access this resource.",
                        "forbidden");
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.AdminOnly, policy =>
                policy.RequireRole(RoleNames.Admin));
        });

        services.AddScoped<IIdentityUserReadService, IdentityUserReadService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<IMfaTicketService, MfaTicketService>();
        var emailOptions = configuration
            .GetSection(IdentityEmailOptions.SectionName)
            .Get<IdentityEmailOptions>() ?? new IdentityEmailOptions();
        ValidateIdentityEmailOptions(emailOptions, environment);
        services.Configure<IdentityEmailOptions>(
            configuration.GetSection(IdentityEmailOptions.SectionName));
        services.AddScoped<IIdentityEmailSender, IdentityEmailSender>();

        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<AssignRoleCommandHandler>();
        services.AddScoped<ChangeUserStatusCommandHandler>();
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<GetUserByIdQueryHandler>();
        services.AddScoped<GetRolesQueryHandler>();
        services.AddScoped<AccountRecoveryHandler>();
        services.AddScoped<MfaFlowHandler>();

        return services;
    }

    private static void ValidateJwtOptions(JwtOptions jwtOptions)
    {
        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
            throw new InvalidOperationException("Jwt:Issuer is required.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
            throw new InvalidOperationException("Jwt:Audience is required.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            throw new InvalidOperationException("Jwt:SecretKey is required.");

        if (Encoding.UTF8.GetByteCount(jwtOptions.SecretKey) < 32)
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 bytes.");

        if (jwtOptions.ExpiryMinutes <= 0)
            throw new InvalidOperationException("Jwt:ExpiryMinutes must be greater than zero.");
    }

    private static void ValidateIdentityEmailOptions(
        IdentityEmailOptions options,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return;

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "IdentityEmail:BaseUrl must be an absolute HTTPS URL outside Development.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress) ||
            string.IsNullOrWhiteSpace(options.SmtpHost) ||
            options.SmtpPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException(
                "IdentityEmail FromAddress, SmtpHost, and a valid SmtpPort are required outside Development.");
        }

        if (string.IsNullOrWhiteSpace(options.SmtpUsername) !=
            string.IsNullOrWhiteSpace(options.SmtpPassword))
        {
            throw new InvalidOperationException(
                "IdentityEmail SmtpUsername and SmtpPassword must be configured together.");
        }
    }

    private static async Task WriteAuthenticationProblemDetailsAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail,
        string errorCode)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: httpContext.RequestAborted);
    }
}
