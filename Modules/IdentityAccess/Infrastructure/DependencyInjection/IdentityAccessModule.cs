using System.IdentityModel.Tokens.Jwt;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.AssignRole;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.ChangeUserStatus;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUserById;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUsers;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetRoles;


namespace Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;

public static class IdentityAccessModule
{
    public static IServiceCollection AddIdentityAccessModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection not found.");

        services.AddDbContext<IdentityAccessDbContext>(options =>
            options.UseNpgsql(connectionString));

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
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var jti = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

                    if (string.IsNullOrWhiteSpace(jti))
                    {
                        context.Fail("Token does not contain jti.");
                        return;
                    }

                    var revocationService = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenRevocationService>();

                    var revoked = await revocationService.IsRevokedAsync(jti);
                    if (revoked)
                    {
                        context.Fail("Token has been revoked.");
                    }
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.AdminOnly, policy =>
                policy.RequireRole(RoleNames.Admin));
        });

        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ITokenRevocationService, TokenRevocationService>();

        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<GetCurrentUserQueryHandler>();
        services.AddScoped<AssignRoleCommandHandler>();
        services.AddScoped<ChangeUserStatusCommandHandler>();
        services.AddScoped<GetUsersQueryHandler>();
        services.AddScoped<GetUserByIdQueryHandler>();
        services.AddScoped<GetRolesQueryHandler>();

        return services;
    }
}