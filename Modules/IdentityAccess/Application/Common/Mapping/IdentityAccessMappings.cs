using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Entities;

namespace Glinter.Modules.IdentityAccess.Application.Common.Mapping;

public static class IdentityAccessMappings
{
    public static AuthResponse ToAuthResponse(
        ApplicationUser user,
        IList<string> roles,
        string token)
    {
        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            Token = token
        };
    }

    public static CurrentUserResponse ToCurrentUserResponse(
        ApplicationUser user,
        IList<string> roles)
    {
        return new CurrentUserResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Roles = roles.ToList()
        };
    }

    public static UserResponse ToUserResponse(
        ApplicationUser user,
        IList<string> roles)
    {
        return new UserResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc,
            Roles = roles.ToList()
        };
    }

    public static UserListItemResponse ToUserListItemResponse(
        ApplicationUser user,
        IList<string> roles)
    {
        return new UserListItemResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? string.Empty
        };
    }

    public static RoleResponse ToRoleResponse(string roleName)
    {
        return new RoleResponse
        {
            Name = roleName
        };
    }
}