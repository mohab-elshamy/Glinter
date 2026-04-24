namespace Glinter.Modules.IdentityAccess.Infrastructure.Identity;

public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = "System Admin";
}