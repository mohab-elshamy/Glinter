using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Glinter.Shared.Application.Auditing;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.ChangeUserStatus;

public class ChangeUserStatusCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthTokenService _authTokenService;
    private readonly AdminAuditDetailsContext _auditDetails;
    private readonly ChangeUserStatusCommandValidator _validator = new();

    public ChangeUserStatusCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthTokenService authTokenService,
        AdminAuditDetailsContext auditDetails)
    {
        _userManager = userManager;
        _authTokenService = authTokenService;
        _auditDetails = auditDetails;
    }

    public async Task<UserResponse> HandleAsync(ChangeUserStatusCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        if (command.IsActive &&
            user.AccountReviewStatus is AccountReviewStatus.Pending or AccountReviewStatus.Rejected)
        {
            throw new ValidationException(
                "Registration must be approved before this user can be activated.");
        }

        var previousIsActive = user.IsActive;
        user.IsActive = command.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ConflictException(string.Join(" | ", result.Errors.Select(x => x.Description)));

        if (!command.IsActive)
        {
            await _authTokenService.RevokeAllAsync(user.Id, "User deactivated");
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                throw new ConflictException(
                    string.Join(" | ", stampResult.Errors.Select(x => x.Description)));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        _auditDetails.SetChanges(
            new { IsActive = previousIsActive },
            new { IsActive = user.IsActive });

        return IdentityAccessMappings.ToUserResponse(user, roles);
    }
}
