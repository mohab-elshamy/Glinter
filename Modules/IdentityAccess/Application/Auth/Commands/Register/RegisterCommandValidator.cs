using Glinter.Modules.IdentityAccess.Domain.Constants;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;

public class RegisterCommandValidator
{
    public List<string> Validate(RegisterCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.FullName))
            errors.Add("Full name is required.");

        if (string.IsNullOrWhiteSpace(command.Email))
            errors.Add("Email is required.");

        if (string.IsNullOrWhiteSpace(command.Password))
            errors.Add("Password is required.");

        if (string.IsNullOrWhiteSpace(command.Role))
            errors.Add("Role is required.");

        if (!RoleNames.PublicRegistration.Contains(command.Role))
            errors.Add("Invalid public registration role.");

        return errors;
    }
}
