namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.ChangeUserStatus;

public class ChangeUserStatusCommandValidator
{
    public List<string> Validate(ChangeUserStatusCommand command)
    {
        var errors = new List<string>();

        if (command.UserId == Guid.Empty)
            errors.Add("User id is required.");

        return errors;
    }
}