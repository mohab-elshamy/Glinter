namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceBooking;

public class CreateExperienceBookingCommandValidator
{
    public void Validate(CreateExperienceBookingCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
            throw new ArgumentException("ExperienceId is required.");

        if (command.AvailabilityId == Guid.Empty)
            throw new ArgumentException("AvailabilityId is required.");

        if (command.GuestsCount <= 0)
            throw new ArgumentException("GuestsCount must be greater than zero.");
    }
}