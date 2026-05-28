namespace Glinter.Modules.Experiences.Application.Experiences.Commands.SetExperienceActiveStatus;

public class SetExperienceActiveStatusCommand
{
    public Guid ExperienceId { get; set; }

    public bool IsActive { get; set; }
}