namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.ChangeUserStatus;

public class ChangeUserStatusCommand
{
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
}