namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class ExperienceAvailabilityResponseDto
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public DateTime StartTimeUtc { get; set; }

    public DateTime EndTimeUtc { get; set; }

    public int Capacity { get; set; }

    public int BookedCount { get; set; }

    public int RemainingCapacity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}