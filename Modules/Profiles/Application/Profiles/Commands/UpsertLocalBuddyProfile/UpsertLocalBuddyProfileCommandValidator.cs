namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertLocalBuddyProfile;

public class UpsertLocalBuddyProfileCommandValidator
{
    public List<string> Validate(UpsertLocalBuddyProfileCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.DisplayName))
            errors.Add("Display name is required.");

        if (command.DisplayName.Length > 200)
            errors.Add("Display name must not exceed 200 characters.");

        if (!string.IsNullOrWhiteSpace(command.Bio) && command.Bio.Length > 1000)
            errors.Add("Bio must not exceed 1000 characters.");

        if (string.IsNullOrWhiteSpace(command.City))
            errors.Add("City is required.");

        if (command.City.Length > 100)
            errors.Add("City must not exceed 100 characters.");

        if (!string.IsNullOrWhiteSpace(command.Languages) && command.Languages.Length > 500)
            errors.Add("Languages must not exceed 500 characters.");

        return errors;
    }
}