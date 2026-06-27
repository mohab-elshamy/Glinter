using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;

public class RegisterCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly RegisterCommandValidator _validator = new();

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> HandleAsync(RegisterCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser is not null)
            throw new ConflictException("Email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = command.FullName,
            Email = command.Email,
            UserName = command.Email,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, command.Password);

        if (!result.Succeeded)
            throw new ValidationException(string.Join(" | ", result.Errors.Select(x => x.Description)));

        var roleResult = await _userManager.AddToRoleAsync(user, command.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            throw new ValidationException(
                string.Join(" | ", roleResult.Errors.Select(x => x.Description)));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenGenerator.GenerateToken(user, roles);

        return IdentityAccessMappings.ToAuthResponse(user, roles, token);
    }
}
