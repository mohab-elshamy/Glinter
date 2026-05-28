namespace Glinter.Modules.Experiences.Application.Experiences.Commands.UpdateExperience;

public class UpdateExperienceCommandValidator
{
    public void Validate(UpdateExperienceCommand command)
    {
        if (command.Id == Guid.Empty)
            throw new ArgumentException("Experience Id is required.");

        if (command.CategoryId == Guid.Empty)
            throw new ArgumentException("CategoryId is required.");

        if (command.AreaId == Guid.Empty)
            throw new ArgumentException("AreaId is required.");

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

        if (string.IsNullOrWhiteSpace(command.Currency))
            throw new ArgumentException("Currency is required.");

        if (command.Currency.Length > 10)
            throw new ArgumentException("Currency cannot exceed 10 characters.");

        if (command.DurationMinutes <= 0)
            throw new ArgumentException("DurationMinutes must be greater than zero.");

        if (command.MaxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than zero.");

        if (command.Latitude < -90 || command.Latitude > 90)
            throw new ArgumentException("Latitude must be between -90 and 90.");

        if (command.Longitude < -180 || command.Longitude > 180)
            throw new ArgumentException("Longitude must be between -180 and 180.");
    }
}