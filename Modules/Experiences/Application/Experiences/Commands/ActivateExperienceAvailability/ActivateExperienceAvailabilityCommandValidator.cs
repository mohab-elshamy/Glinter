namespace Glinter.Modules.Experiences.Application.Experiences.Commands.ActivateExperienceAvailability;

public class ActivateExperienceAvailabilityCommandValidator
{
    public void Validate(ActivateExperienceAvailabilityCommand command)
    {
        if (command.AvailabilityId == Guid.Empty)
        {
            throw new ArgumentException("AvailabilityId is required.");
        }
    }
}