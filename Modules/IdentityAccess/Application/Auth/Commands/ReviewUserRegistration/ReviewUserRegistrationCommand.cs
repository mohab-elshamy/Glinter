namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.ReviewUserRegistration;

public sealed class ReviewUserRegistrationCommand
{
    public Guid UserId { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
