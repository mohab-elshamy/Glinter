using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetRoles;

public class GetRolesQueryHandler
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public GetRolesQueryHandler(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<List<RoleResponse>> HandleAsync(GetRolesQuery query)
    {
        var roles = await _roleManager.Roles
            .OrderBy(x => x.Name)
            .Select(x => x.Name!)
            .ToListAsync();

        return roles.Select(IdentityAccessMappings.ToRoleResponse).ToList();
    }
}