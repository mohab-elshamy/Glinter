namespace Glinter.Modules.Experiences.Application.Experiences.Commands.DeactivateExperienceAvailability;

public class DeactivateExperienceAvailabilityCommandValidator
{
    public void Validate(DeactivateExperienceAvailabilityCommand command)
    {
        if (command.AvailabilityId == Guid.Empty)
        {
            throw new ArgumentException("AvailabilityId is required.");
        }
    }
}