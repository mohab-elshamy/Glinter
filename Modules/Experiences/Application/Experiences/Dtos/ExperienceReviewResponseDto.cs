namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class ExperienceReviewResponseDto
{
    public Guid Id { get; set; }

    public Guid ExperienceId { get; set; }

    public Guid TravelerProfileId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}