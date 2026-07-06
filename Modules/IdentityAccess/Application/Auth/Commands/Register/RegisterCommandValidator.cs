using Glinter.Modules.IdentityAccess.Domain.Constants;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.Register;

public class RegisterCommandValidator
{
    private static readonly string[] RolesRequiringReview =
    [
        RoleNames.LocalBuddy,
        RoleNames.HotelOwner,
        RoleNames.ExperienceProvider
    ];

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

        if (RolesRequiringReview.Contains(command.Role))
        {
            if (string.IsNullOrWhiteSpace(command.IdentityDocumentUrl))
                errors.Add("Identity document is required for this account type.");

            if (string.IsNullOrWhiteSpace(command.IdentityDocumentFileName))
                errors.Add("Identity document file name is required.");

            if (string.IsNullOrWhiteSpace(command.IdentityDocumentContentType))
                errors.Add("Identity document content type is required.");
        }

        if (!string.IsNullOrWhiteSpace(command.IdentityDocumentUrl) &&
            command.IdentityDocumentUrl.Length > 7_000_000)
        {
            errors.Add("Identity document is too large.");
        }

        if (!string.IsNullOrWhiteSpace(command.IdentityDocumentFileName) &&
            command.IdentityDocumentFileName.Length > 260)
        {
            errors.Add("Identity document file name is too long.");
        }

        if (!string.IsNullOrWhiteSpace(command.IdentityDocumentContentType) &&
            command.IdentityDocumentContentType.Length > 100)
        {
            errors.Add("Identity document content type is too long.");
        }

        return errors;
    }
}
