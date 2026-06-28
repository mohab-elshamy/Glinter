using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Modules.IdentityAccess.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Login;

public class LoginCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAuthTokenService _authTokenService;
    private readonly IMfaTicketService _mfaTicketService;
    private readonly LoginCommandValidator _validator = new();

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAuthTokenService authTokenService,
        IMfaTicketService mfaTicketService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authTokenService = authTokenService;
        _mfaTicketService = mfaTicketService;
    }

    public async Task<object> HandleAsync(LoginCommand command)
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

        if (result.IsNotAllowed && !user.EmailConfirmed)
            throw new AuthenticationException("Email confirmation is required.");

        if (!result.Succeeded)
            throw new AuthenticationException("Invalid email or password.");

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(RoleNames.Admin))
        {
            var purpose = user.TwoFactorEnabled ? "verify" : "setup";
            var ticket = await _mfaTicketService.CreateAsync(user.Id, purpose);
            return new MfaChallengeResponse
            {
                RequiresSetup = !user.TwoFactorEnabled,
                MfaTicket = ticket.Value,
                ExpiresAtUtc = ticket.ExpiresAtUtc
            };
        }

        return await _authTokenService.IssueAsync(user, roles);
    }
}
