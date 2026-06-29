using System.Text.Json.Serialization;
using Glinter.Modules.Communication.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Infrastructure.Identity;
using Glinter.Modules.Experiences.Infrastructure.DependencyInjection;
using Glinter.Modules.Profiles.Infrastructure.DependencyInjection;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Regions.Infrastructure.DependencyInjection;
using Glinter.Modules.SafetyIndex.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Module 1: IdentityAccess
builder.Services.AddIdentityAccessModule(builder.Configuration);

// Module 2: Profiles
builder.Services.AddProfilesModule(builder.Configuration);

// Module 4: Stays

// Module 5: Experiences
builder.Services.AddExperiencesModule(builder.Configuration);

// Module 6: Regions (Administrative Boundaries)
builder.Services.AddRegionsModule(builder.Configuration);

// Module 7: Safety Index
builder.Services.AddSafetyIndexModule(builder.Configuration);

// Module 9: Communication
builder.Services.AddCommunicationModule(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Glinter API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

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

app.Run();
