using System.Text.Json.Serialization;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Infrastructure.Identity;
using Glinter.Modules.Profiles.Infrastructure.DependencyInjection;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Glinter.Modules.Stays.Infrastructure.DependencyInjection;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Module 1: IdentityAccess
builder.Services.AddIdentityAccessModule(builder.Configuration);

// Module 2: Profiles
builder.Services.AddProfilesModule(builder.Configuration);

// Module 4: Stays
builder.Services.AddDbContext<StaysDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStaysModule();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

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

app.Run();