namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;

public class RegisterCommand
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Traveler";
}