using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Infrastructure.DependencyInjection;
using Glinter.Modules.IdentityAccess.Infrastructure.Identity;
using Glinter.Modules.Stays.Infrastructure.DependencyInjection;
using Glinter.Modules.Stays.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Glinter;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });

        builder.Services.AddDbContext<StaysDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentityAccessModule(builder.Configuration);
        builder.Services.AddStaysModule();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

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

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}