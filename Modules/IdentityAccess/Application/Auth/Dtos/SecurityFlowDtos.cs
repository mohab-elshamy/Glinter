namespace Glinter.Modules.IdentityAccess.Application.Auth.Dtos;

public sealed class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DevelopmentConfirmationToken { get; set; }
}

public sealed class MessageResponse
{
    public string Message { get; set; } = string.Empty;
    public string? DevelopmentToken { get; set; }
}

public sealed class ConfirmEmailRequest
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
}

public sealed class EmailRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class MfaChallengeResponse
{
    public bool RequiresMfa { get; set; } = true;
    public bool RequiresSetup { get; set; }
    public string MfaTicket { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public class MfaTicketRequest
{
    public string MfaTicket { get; set; } = string.Empty;
}

public sealed class EnableMfaRequest : MfaTicketRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed class VerifyMfaRequest : MfaTicketRequest
{
    public string? Code { get; set; }
    public string? RecoveryCode { get; set; }
}

public sealed class MfaSetupResponse
{
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string MfaTicket { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class EnableMfaResponse
{
    public AuthResponse Authentication { get; set; } = null!;
    public IReadOnlyList<string> RecoveryCodes { get; set; } = [];
}
