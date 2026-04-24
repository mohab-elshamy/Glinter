using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AdminSeedOptions adminOptions)
    {
        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpper()
                });
            }
        }

        if (string.IsNullOrWhiteSpace(adminOptions.Email))
            throw new InvalidOperationException("Admin seed email is missing.");

        if (string.IsNullOrWhiteSpace(adminOptions.Password))
            throw new InvalidOperationException("Admin seed password is missing.");

        var adminUser = await userManager.FindByEmailAsync(adminOptions.Email);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = adminOptions.FullName,
                Email = adminOptions.Email,
                UserName = adminOptions.Email,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminOptions.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to seed admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, RoleNames.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(" | ", roleResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to assign admin role: {errors}");
            }
        }
    }
}