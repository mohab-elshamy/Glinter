using System.Text.Json.Serialization;
using Glinter.Modules.Experiences.Infrastructure.DependencyInjection;
using Glinter.Modules.Experiences.Infrastructure.Persistence;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Infrastructure.Identity;
using Glinter.Modules.Profiles.Infrastructure.DependencyInjection;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.DependencyInjection;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Module 1: IdentityAccess
builder.Services.AddIdentityAccessModule(builder.Configuration);

// Module 2: Profiles
builder.Services.AddProfilesModule(builder.Configuration);

// Module 4: Stays
builder.Services.AddDbContext<StaysDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStaysModule();

// Module 5: Experiences
builder.Services.AddExperiencesModule(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });


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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Glinter API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

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

using (var scope = app.Services.CreateScope())
{
    var experiencesDbContext = scope.ServiceProvider.GetRequiredService<ExperiencesDbContext>();
    await ExperiencesSeeder.SeedAsync(experiencesDbContext);
}

app.Run();