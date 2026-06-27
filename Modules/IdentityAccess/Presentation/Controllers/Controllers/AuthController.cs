using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Login;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;
using Glinter.Modules.IdentityAccess.Application.Auth.Queries.GetCurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Glinter.Modules.IdentityAccess.Application.Auth.Commands.Logout;

namespace Glinter.Modules.IdentityAccess.Presentation.Controllers.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterCommandHandler _registerCommandHandler;
    private readonly LoginCommandHandler _loginCommandHandler;
    private readonly GetCurrentUserQueryHandler _getCurrentUserQueryHandler;
    private readonly LogoutCommandHandler _logoutCommandHandler;

    public AuthController(
        RegisterCommandHandler registerCommandHandler,
        LoginCommandHandler loginCommandHandler,
        LogoutCommandHandler logoutCommandHandler,
        GetCurrentUserQueryHandler getCurrentUserQueryHandler)
    {
        _registerCommandHandler = registerCommandHandler;
        _loginCommandHandler = loginCommandHandler;
        _logoutCommandHandler = logoutCommandHandler;
        _getCurrentUserQueryHandler = getCurrentUserQueryHandler;
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