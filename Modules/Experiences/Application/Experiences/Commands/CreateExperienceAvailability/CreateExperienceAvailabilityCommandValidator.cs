namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceAvailability;

public class CreateExperienceAvailabilityCommandValidator
{
    public void Validate(CreateExperienceAvailabilityCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
            throw new ValidationException("ExperienceId is required.");

        if (command.StartTimeUtc <= DateTime.UtcNow)
            throw new ValidationException("StartTimeUtc must be in the future.");

        if (command.EndTimeUtc <= command.StartTimeUtc)
            throw new ValidationException("EndTimeUtc must be after StartTimeUtc.");

        if (command.Capacity <= 0)
            throw new ValidationException("Capacity must be greater than zero.");
    }
}