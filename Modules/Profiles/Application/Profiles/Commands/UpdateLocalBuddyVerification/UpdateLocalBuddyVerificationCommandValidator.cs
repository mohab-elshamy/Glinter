using Glinter.Modules.Profiles.Domain.Enums;

namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpdateLocalBuddyVerification;

public class UpdateLocalBuddyVerificationCommandValidator
{
    public List<string> Validate(UpdateLocalBuddyVerificationCommand command)
    {
        var errors = new List<string>();

        if (command.UserId == Guid.Empty)
            errors.Add("User id is required.");

        if (string.IsNullOrWhiteSpace(command.VerificationStatus))
        {
            errors.Add("Verification status is required.");
            return errors;
        }

        var isValidStatus = Enum.TryParse<VerificationStatus>(
            command.VerificationStatus,
            ignoreCase: true,
            out _);

        if (!isValidStatus)
            errors.Add("Verification status must be Pending, Approved, or Rejected.");

        return errors;
    }
}