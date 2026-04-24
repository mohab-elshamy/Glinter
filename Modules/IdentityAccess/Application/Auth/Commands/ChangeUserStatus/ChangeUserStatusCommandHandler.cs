using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.ChangeUserStatus;

public class ChangeUserStatusCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ChangeUserStatusCommandValidator _validator = new();

    public ChangeUserStatusCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserResponse> HandleAsync(ChangeUserStatusCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" | ", errors));

        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
            throw new KeyNotFoundException("User not found.");

        user.IsActive = command.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" | ", result.Errors.Select(x => x.Description)));

        var roles = await _userManager.GetRolesAsync(user);

        return IdentityAccessMappings.ToUserResponse(user, roles);
    }
}