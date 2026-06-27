using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUserById;

public class GetUserByIdQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUserByIdQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserResponse> HandleAsync(GetUserByIdQuery query)
    {
        if (query.UserId == Guid.Empty)
            throw new ValidationException("User id is required.");

        var user = await _userManager.FindByIdAsync(query.UserId.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        return IdentityAccessMappings.ToUserResponse(user, roles);
    }
}