namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceActiveStatus;

public class SetExperienceActiveStatusCommandValidator
{
    public void Validate(SetExperienceActiveStatusCommand command)
    {
        if (command.ExperienceId == Guid.Empty)
        {
            throw new ArgumentException("ExperienceId is required.");
        }
    }
}