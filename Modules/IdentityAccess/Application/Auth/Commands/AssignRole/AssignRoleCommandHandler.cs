using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.AssignRole;

public class AssignRoleCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AssignRoleCommandValidator _validator = new();

    public AssignRoleCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserResponse> HandleAsync(AssignRoleCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
            throw new KeyNotFoundException("User not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);

        if (currentRoles.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
                throw new InvalidOperationException(string.Join(" | ", removeResult.Errors.Select(x => x.Description)));
        }

        var addResult = await _userManager.AddToRoleAsync(user, command.Role);
        if (!addResult.Succeeded)
            throw new InvalidOperationException(string.Join(" | ", addResult.Errors.Select(x => x.Description)));

        var updatedRoles = await _userManager.GetRolesAsync(user);

        return IdentityAccessMappings.ToUserResponse(user, updatedRoles);
    }
}