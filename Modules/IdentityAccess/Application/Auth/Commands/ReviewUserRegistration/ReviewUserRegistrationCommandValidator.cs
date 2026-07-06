using Glinter.Modules.IdentityAccess.Domain.Enums;

namespace Glinter.Modules.IdentityAccess.Application.Auth.Commands.ReviewUserRegistration;

public sealed class ReviewUserRegistrationCommandValidator
{
    public List<string> Validate(ReviewUserRegistrationCommand command)
    {
        var errors = new List<string>();

        if (command.UserId == Guid.Empty)
            errors.Add("User id is required.");

        if (!Enum.TryParse<AccountReviewStatus>(
                command.ReviewStatus,
                ignoreCase: true,
                out var status) ||
            status is not (AccountReviewStatus.Approved or AccountReviewStatus.Rejected))
        {
            errors.Add("Review status must be Approved or Rejected.");
        }

        if (!string.IsNullOrWhiteSpace(command.Notes) && command.Notes.Length > 1000)
            errors.Add("Review notes cannot exceed 1000 characters.");

        return errors;
    }
}
