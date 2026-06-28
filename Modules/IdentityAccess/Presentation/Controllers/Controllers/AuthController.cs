using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Login;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetCurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Logout;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.SecurityFlows;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.IdentityAccess.Presentation.Controllers.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterCommandHandler _registerCommandHandler;
    private readonly LoginCommandHandler _loginCommandHandler;
    private readonly GetCurrentUserQueryHandler _getCurrentUserQueryHandler;
    private readonly LogoutCommandHandler _logoutCommandHandler;
    private readonly AccountRecoveryHandler _accountRecoveryHandler;
    private readonly MfaFlowHandler _mfaFlowHandler;
    private readonly IAuthTokenService _authTokenService;

    public AuthController(
        RegisterCommandHandler registerCommandHandler,
        LoginCommandHandler loginCommandHandler,
        LogoutCommandHandler logoutCommandHandler,
        GetCurrentUserQueryHandler getCurrentUserQueryHandler,
        AccountRecoveryHandler accountRecoveryHandler,
        MfaFlowHandler mfaFlowHandler,
        IAuthTokenService authTokenService)
    {
        _registerCommandHandler = registerCommandHandler;
        _loginCommandHandler = loginCommandHandler;
        _logoutCommandHandler = logoutCommandHandler;
        _getCurrentUserQueryHandler = getCurrentUserQueryHandler;
        _accountRecoveryHandler = accountRecoveryHandler;
        _mfaFlowHandler = mfaFlowHandler;
        _authTokenService = authTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _registerCommandHandler.HandleAsync(new RegisterCommand
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password,
            Role = request.Role
        });

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _loginCommandHandler.HandleAsync(new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        });

        return Ok(result);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request) =>
        Ok(await _accountRecoveryHandler.ConfirmEmailAsync(request));

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation([FromBody] EmailRequest request) =>
        Ok(await _accountRecoveryHandler.ResendConfirmationAsync(request));

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] EmailRequest request) =>
        Ok(await _accountRecoveryHandler.ForgotPasswordAsync(request));

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request) =>
        Ok(await _accountRecoveryHandler.ResetPasswordAsync(request));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authTokenService.RotateAsync(request.RefreshToken, cancellationToken));

    [HttpPost("mfa/setup")]
    public async Task<IActionResult> SetupMfa([FromBody] MfaTicketRequest request) =>
        Ok(await _mfaFlowHandler.SetupAsync(request));

    [HttpPost("mfa/enable")]
    public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequest request) =>
        Ok(await _mfaFlowHandler.EnableAsync(request));

    [HttpPost("mfa/verify")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request) =>
        Ok(await _mfaFlowHandler.VerifyAsync(request));

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var result = await _getCurrentUserQueryHandler.HandleAsync(new GetCurrentUserQuery());
        return Ok(result);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await _logoutCommandHandler.HandleAsync(new LogoutCommand());
        return Ok(result);
    }
}
