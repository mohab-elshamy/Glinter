using Glinter.Modules.IdentityAccess.Application.Auth.Commands.AssignRole;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.ChangeUserStatus;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.ReviewUserRegistration;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetRoles;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUserById;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetUsers;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.IdentityAccess.Presentation.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.AdminOnly)]
public class AdminUsersController : ControllerBase
{
    private readonly GetUsersQueryHandler _getUsersQueryHandler;
    private readonly GetUserByIdQueryHandler _getUserByIdQueryHandler;
    private readonly GetRolesQueryHandler _getRolesQueryHandler;
    private readonly AssignRoleCommandHandler _assignRoleCommandHandler;
    private readonly ChangeUserStatusCommandHandler _changeUserStatusCommandHandler;
    private readonly ReviewUserRegistrationCommandHandler _reviewUserRegistrationCommandHandler;

    public AdminUsersController(
        GetUsersQueryHandler getUsersQueryHandler,
        GetUserByIdQueryHandler getUserByIdQueryHandler,
        GetRolesQueryHandler getRolesQueryHandler,
        AssignRoleCommandHandler assignRoleCommandHandler,
        ChangeUserStatusCommandHandler changeUserStatusCommandHandler,
        ReviewUserRegistrationCommandHandler reviewUserRegistrationCommandHandler)
    {
        _getUsersQueryHandler = getUsersQueryHandler;
        _getUserByIdQueryHandler = getUserByIdQueryHandler;
        _getRolesQueryHandler = getRolesQueryHandler;
        _assignRoleCommandHandler = assignRoleCommandHandler;
        _changeUserStatusCommandHandler = changeUserStatusCommandHandler;
        _reviewUserRegistrationCommandHandler = reviewUserRegistrationCommandHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _getUsersQueryHandler.HandleAsync(new GetUsersQuery());
        return Ok(result);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _getRolesQueryHandler.HandleAsync(new GetRolesQuery());
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _getUserByIdQueryHandler.HandleAsync(new GetUserByIdQuery
        {
            UserId = id
        });

        return Ok(result);
    }

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        var result = await _assignRoleCommandHandler.HandleAsync(new AssignRoleCommand
        {
            UserId = id,
            Role = request.Role
        });

        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeUserStatus(Guid id, [FromBody] ChangeUserStatusRequest request)
    {
        var result = await _changeUserStatusCommandHandler.HandleAsync(new ChangeUserStatusCommand
        {
            UserId = id,
            IsActive = request.IsActive
        });

        return Ok(result);
    }

    [HttpPatch("{id:guid}/registration-review")]
    public async Task<IActionResult> ReviewRegistration(
        Guid id,
        [FromBody] ReviewUserRegistrationRequest request)
    {
        var result = await _reviewUserRegistrationCommandHandler.HandleAsync(new ReviewUserRegistrationCommand
        {
            UserId = id,
            ReviewStatus = request.ReviewStatus,
            Notes = request.Notes
        });

        return Ok(result);
    }
}
