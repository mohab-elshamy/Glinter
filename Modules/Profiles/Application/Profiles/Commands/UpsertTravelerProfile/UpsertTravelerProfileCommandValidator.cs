namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertTravelerProfile;

public class UpsertTravelerProfileCommandValidator
{
    public List<string> Validate(UpsertTravelerProfileCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.DisplayName))
            errors.Add("Display name is required.");

        if (command.DisplayName.Length > 200)
            errors.Add("Display name must not exceed 200 characters.");

        if (!string.IsNullOrWhiteSpace(command.Bio) && command.Bio.Length > 1000)
            errors.Add("Bio must not exceed 1000 characters.");

        if (!string.IsNullOrWhiteSpace(command.Nationality) && command.Nationality.Length > 100)
            errors.Add("Nationality must not exceed 100 characters.");

        if (!string.IsNullOrWhiteSpace(command.PreferredBudgetLevel) && command.PreferredBudgetLevel.Length > 50)
            errors.Add("Preferred budget level must not exceed 50 characters.");

        if (!string.IsNullOrWhiteSpace(command.TravelStyle) && command.TravelStyle.Length > 100)
            errors.Add("Travel style must not exceed 100 characters.");

        if (!string.IsNullOrWhiteSpace(command.PreferredInterests) && command.PreferredInterests.Length > 1000)
            errors.Add("Preferred interests must not exceed 1000 characters.");

        return errors;
    }
}