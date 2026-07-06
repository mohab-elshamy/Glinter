using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Glinter.Modules.IdentityAccess.Domain.Enums;
using Glinter.Shared.Application.Auditing;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.ReviewUserRegistration;

public sealed class ReviewUserRegistrationCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthTokenService _authTokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly AdminAuditDetailsContext _auditDetails;
    private readonly ReviewUserRegistrationCommandValidator _validator = new();

    public ReviewUserRegistrationCommandHandler(
        UserManager<ApplicationUser> userManager,
        IAuthTokenService authTokenService,
        ICurrentUserService currentUser,
        AdminAuditDetailsContext auditDetails)
    {
        _userManager = userManager;
        _authTokenService = authTokenService;
        _currentUser = currentUser;
        _auditDetails = auditDetails;
    }

    public async Task<UserResponse> HandleAsync(ReviewUserRegistrationCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        if (user.AccountReviewStatus == AccountReviewStatus.NotRequired)
            throw new ValidationException("This user does not require registration review.");

        var reviewStatus = Enum.Parse<AccountReviewStatus>(
            command.ReviewStatus,
            ignoreCase: true);

        var previous = new
        {
            user.IsActive,
            AccountReviewStatus = user.AccountReviewStatus.ToString(),
            user.AccountReviewNotes,
            user.AccountReviewedByUserId,
            user.AccountReviewedAtUtc
        };

        user.AccountReviewStatus = reviewStatus;
        user.IsActive = reviewStatus == AccountReviewStatus.Approved;
        user.AccountReviewNotes = string.IsNullOrWhiteSpace(command.Notes)
            ? null
            : command.Notes.Trim();
        user.AccountReviewedByUserId = _currentUser.UserId;
        user.AccountReviewedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ConflictException(string.Join(" | ", result.Errors.Select(x => x.Description)));

        if (!user.IsActive)
        {
            await _authTokenService.RevokeAllAsync(user.Id, "Registration rejected");
            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                throw new ConflictException(
                    string.Join(" | ", stampResult.Errors.Select(x => x.Description)));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        _auditDetails.SetChanges(previous, new
        {
            user.IsActive,
            AccountReviewStatus = user.AccountReviewStatus.ToString(),
            user.AccountReviewNotes,
            user.AccountReviewedByUserId,
            user.AccountReviewedAtUtc
        });

        return IdentityAccessMappings.ToUserResponse(user, roles);
    }
}
