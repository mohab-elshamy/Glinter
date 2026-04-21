using Glinter.Modules.IdentityAccess.Domain.Constants;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.AssignRole;

public class AssignRoleCommandValidator
{
    public List<string> Validate(AssignRoleCommand command)
    {
        var errors = new List<string>();

        if (command.UserId == Guid.Empty)
            errors.Add("User id is required.");

        if (string.IsNullOrWhiteSpace(command.Role))
            errors.Add("Role is required.");

        if (!RoleNames.All.Contains(command.Role))
            errors.Add("Invalid role.");

        return errors;
    }
}