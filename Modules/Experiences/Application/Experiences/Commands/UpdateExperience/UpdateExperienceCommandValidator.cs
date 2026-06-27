namespace Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperience;

public class UpdateExperienceCommandValidator
{
    private const decimal MaxDatabaseMoneyValue = 9_999_999_999_999_999.99m;
    private const int MaxTags = 50;
    private const int MaxVibes = 20;

    public void Validate(UpdateExperienceCommand command)
    {
        if (command.Id == Guid.Empty)
            throw new ValidationException("Experience Id is required.");

        if (command.CategoryId == Guid.Empty)
            throw new ValidationException("CategoryId is required.");

        if (command.Adm3Gid <= 0)
            throw new ValidationException("Adm3Gid is required.");

        if (string.IsNullOrWhiteSpace(command.Title))
            throw new ValidationException("Title is required.");

        if (command.Title.Length > 200)
            throw new ValidationException("Title cannot exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(command.Description))
            throw new ValidationException("Description is required.");

        if (command.Description.Length > 3000)
            throw new ValidationException("Description cannot exceed 3000 characters.");

        if (string.IsNullOrWhiteSpace(command.LocationName))
            throw new ValidationException("LocationName is required.");

        if (command.LocationName.Length > 300)
            throw new ValidationException("LocationName cannot exceed 300 characters.");

        if (command.PricePerPerson <= 0)
            throw new ValidationException("PricePerPerson must be greater than zero.");

        if (command.PricePerPerson > MaxDatabaseMoneyValue)
            throw new ValidationException("PricePerPerson exceeds the maximum supported value.");

        if (string.IsNullOrWhiteSpace(command.Currency))
            throw new ValidationException("Currency is required.");

        if (command.Currency.Length > 10)
            throw new ValidationException("Currency cannot exceed 10 characters.");

        if (command.DurationMinutes <= 0)
            throw new ValidationException("DurationMinutes must be greater than zero.");

        if (command.MaxGuests <= 0)
            throw new ValidationException("MaxGuests must be greater than zero.");

        if (double.IsNaN(command.Latitude) ||
            double.IsInfinity(command.Latitude) ||
            command.Latitude < -90 ||
            command.Latitude > 90)
            throw new ValidationException("Latitude must be between -90 and 90.");

        if (double.IsNaN(command.Longitude) ||
            double.IsInfinity(command.Longitude) ||
            command.Longitude < -180 ||
            command.Longitude > 180)
            throw new ValidationException("Longitude must be between -180 and 180.");

        if (command.Tags is null)
            throw new ValidationException("Tags cannot be null.");

        if (command.Tags.Count > MaxTags)
            throw new ValidationException($"An experience cannot have more than {MaxTags} tags.");

        if (command.Tags.Any(tag =>
                !string.IsNullOrWhiteSpace(tag) &&
                tag.Trim().Length > 100))
            throw new ValidationException("Tags cannot exceed 100 characters.");

        if (command.VibeIds is null)
            throw new ValidationException("VibeIds cannot be null.");

        if (command.VibeIds.Count > MaxVibes)
            throw new ValidationException($"An experience cannot have more than {MaxVibes} vibes.");

        if (command.VibeIds.Any(id => id == Guid.Empty))
            throw new ValidationException("VibeIds must contain valid ids.");
    }
}
