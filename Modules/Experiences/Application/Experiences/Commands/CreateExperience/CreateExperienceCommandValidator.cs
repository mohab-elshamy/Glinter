namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperience;

public class CreateExperienceCommandValidator
{
    private const decimal MaxDatabaseMoneyValue = 9_999_999_999_999_999.99m;
    private const int MaxTags = 50;
    private const int MaxVibes = 20;

    public void Validate(CreateExperienceCommand command)
    {

        if (command.CategoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.");

        if (command.Adm3Gid <= 0)
            throw new ArgumentException("Adm3Gid is required.");

        if (string.IsNullOrWhiteSpace(command.Title))
            throw new ArgumentException("Title is required.");

        if (command.Title.Length > 200)
            throw new ArgumentException("Title cannot exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(command.Description))
            throw new ArgumentException("Description is required.");

        if (command.Description.Length > 3000)
            throw new ArgumentException("Description cannot exceed 3000 characters.");

        if (string.IsNullOrWhiteSpace(command.LocationName))
            throw new ArgumentException("LocationName is required.");

        if (command.LocationName.Length > 300)
            throw new ArgumentException("LocationName cannot exceed 300 characters.");

        if (command.PricePerPerson <= 0)
            throw new ArgumentException("PricePerPerson must be greater than zero.");

        if (command.PricePerPerson > MaxDatabaseMoneyValue)
            throw new ArgumentException("PricePerPerson exceeds the maximum supported value.");

        if (string.IsNullOrWhiteSpace(command.Currency))
            throw new ArgumentException("Currency is required.");

        if (command.Currency.Length > 10)
            throw new ArgumentException("Currency cannot exceed 10 characters.");

        if (command.DurationMinutes <= 0)
            throw new ArgumentException("DurationMinutes must be greater than zero.");

        if (command.MaxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than zero.");

        if (double.IsNaN(command.Latitude) ||
            double.IsInfinity(command.Latitude) ||
            command.Latitude < -90 ||
            command.Latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.");

        if (double.IsNaN(command.Longitude) ||
            double.IsInfinity(command.Longitude) ||
            command.Longitude < -180 ||
            command.Longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.");

        if (command.Tags is null)
            throw new ArgumentException("Tags cannot be null.");

        if (command.Tags.Count > MaxTags)
            throw new ArgumentException($"An experience cannot have more than {MaxTags} tags.");

        if (command.Tags.Any(tag =>
                !string.IsNullOrWhiteSpace(tag) &&
                tag.Trim().Length > 100))
            throw new ArgumentException("Tags cannot exceed 100 characters.");

        if (command.VibeIds is null)
            throw new ArgumentException("VibeIds cannot be null.");

        if (command.VibeIds.Count > MaxVibes)
            throw new ArgumentException($"An experience cannot have more than {MaxVibes} vibes.");

        if (command.VibeIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("VibeIds must contain valid ids.");
    }
}
