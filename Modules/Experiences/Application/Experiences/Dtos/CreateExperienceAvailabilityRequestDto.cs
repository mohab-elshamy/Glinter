namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class CreateExperienceAvailabilityRequestDto
{
    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public int Capacity { get; set; }
}