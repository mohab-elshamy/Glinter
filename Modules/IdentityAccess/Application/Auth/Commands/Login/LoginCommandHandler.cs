using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Common.Mapping;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Login;

public class LoginCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly LoginCommandValidator _validator = new();

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> HandleAsync(LoginCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user is null)
            throw new AuthenticationException("Invalid email or password.");

        if (!user.IsActive)
            throw new AuthenticationException("User is inactive.");

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            command.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
            throw new AuthenticationException("User is temporarily locked. Try again later.");

        if (!result.Succeeded)
            throw new AuthenticationException("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtTokenGenerator.GenerateToken(user, roles);

        return IdentityAccessMappings.ToAuthResponse(user, roles, token);
    }
}
