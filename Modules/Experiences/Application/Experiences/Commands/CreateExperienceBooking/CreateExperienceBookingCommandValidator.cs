namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceBooking;

public class CreateExperienceBookingCommandValidator
{
    public void Validate(CreateExperienceBookingCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
            throw new ValidationException("ExperienceId is required.");

        if (command.AvailabilityId == Guid.Empty)
            throw new ValidationException("AvailabilityId is required.");

        if (command.GuestsCount <= 0)
            throw new ValidationException("GuestsCount must be greater than zero.");
    }
}