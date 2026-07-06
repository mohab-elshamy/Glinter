namespace Glinter.Modules.IdentityAccess.Application.Auth.Dtos;

public sealed class ReviewUserRegistrationRequest
{
    public string ReviewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
