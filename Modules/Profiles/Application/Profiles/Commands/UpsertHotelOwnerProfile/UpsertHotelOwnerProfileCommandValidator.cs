namespace Glinter.Modules.Profiles.Application.Profiles.Commands.UpsertHotelOwnerProfile;

public class UpsertHotelOwnerProfileCommandValidator
{
    public List<string> Validate(UpsertHotelOwnerProfileCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.BusinessName))
            errors.Add("Business name is required.");

        if (command.BusinessName.Length > 200)
            errors.Add("Business name must not exceed 200 characters.");

        if (!string.IsNullOrWhiteSpace(command.ContactPersonName) && command.ContactPersonName.Length > 200)
            errors.Add("Contact person name must not exceed 200 characters.");

        if (!string.IsNullOrWhiteSpace(command.PhoneNumber) && command.PhoneNumber.Length > 50)
            errors.Add("Phone number must not exceed 50 characters.");

        if (!string.IsNullOrWhiteSpace(command.Description) && command.Description.Length > 1000)
            errors.Add("Description must not exceed 1000 characters.");

        return errors;
    }
}