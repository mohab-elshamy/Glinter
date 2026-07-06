namespace Glinter.Modules.IdentityAccess.Application.Auth.Dtos;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Traveler";
    public string? IdentityDocumentUrl { get; set; }
    public string? IdentityDocumentFileName { get; set; }
    public string? IdentityDocumentContentType { get; set; }
}
