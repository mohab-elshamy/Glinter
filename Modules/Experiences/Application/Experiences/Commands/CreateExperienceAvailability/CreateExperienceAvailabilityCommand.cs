namespace Glinter.Modules.Experiences.Application.Experiences.Commands.CreateExperienceAvailability;

public class CreateExperienceAvailabilityCommand
{
    public Guid ExperienceId { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public int Capacity { get; set; }
}