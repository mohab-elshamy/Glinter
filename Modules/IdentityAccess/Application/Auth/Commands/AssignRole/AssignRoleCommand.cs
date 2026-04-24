namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.AssignRole;

public class AssignRoleCommand
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = string.Empty;
}