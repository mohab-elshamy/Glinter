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
        else if (!await userManager.CheckPasswordAsync(adminUser, adminOptions.Password))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            var resetResult = await userManager.ResetPasswordAsync(
                adminUser,
                resetToken,
                adminOptions.Password);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join(" | ", resetResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to reset seeded admin password: {errors}");
            }
        }

        if (await userManager.IsLockedOutAsync(adminUser))
        {
            var unlockResult = await userManager.SetLockoutEndDateAsync(adminUser, null);

            if (!unlockResult.Succeeded)
            {
                var errors = string.Join(" | ", unlockResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Failed to unlock seeded admin user: {errors}");
            }
        }

        var accessFailedResetResult = await userManager.ResetAccessFailedCountAsync(adminUser);

        if (!accessFailedResetResult.Succeeded)
        {
            var errors = string.Join(" | ", accessFailedResetResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to reset seeded admin access failures: {errors}");
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
