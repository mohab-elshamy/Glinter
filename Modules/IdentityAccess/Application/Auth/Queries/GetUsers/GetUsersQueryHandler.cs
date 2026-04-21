using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUsers;

public class GetUsersQueryHandler
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUsersQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<List<UserListItemResponse>> HandleAsync(GetUsersQuery query)
    {
        var users = await _userManager.Users
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync();

        var result = new List<UserListItemResponse>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(IdentityAccessMappings.ToUserListItemResponse(user, roles));
        }

        return result;
    }
}