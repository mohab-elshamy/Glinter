using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Auth.Dtos;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Logout;

public class LogoutCommandHandler
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly IAuthTokenService _authTokenService;
    private readonly LogoutCommandValidator _validator = new();

    public LogoutCommandHandler(
        ICurrentUserService currentUserService,
        ITokenRevocationService tokenRevocationService,
        IAuthTokenService authTokenService)
    {
        _currentUserService = currentUserService;
        _tokenRevocationService = tokenRevocationService;
        _authTokenService = authTokenService;
    }

    public async Task<LogoutResponse> HandleAsync(LogoutCommand command)
    {
        var errors = _validator.Validate(command);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" | ", errors));

        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        await _tokenRevocationService.RevokeCurrentTokenAsync("User logout");
        await _authTokenService.RevokeAllAsync(
            _currentUserService.UserId.Value,
            "User logout");

        return new LogoutResponse
        {
            Message = "Logged out successfully."
        };
    }
}
