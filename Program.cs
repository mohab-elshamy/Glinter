using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Infrastructure.Identity;
using Glinter.Modules.Profiles.Infrastructure.DependencyInjection;
using Glinter.Modules.Profiles.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityAccessModule(builder.Configuration);
builder.Services.AddProfilesModule(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

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