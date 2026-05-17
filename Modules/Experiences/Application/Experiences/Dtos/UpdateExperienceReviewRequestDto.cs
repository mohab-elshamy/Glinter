namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class UpdateExperienceReviewRequestDto
{
    public Guid TravelerProfileId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }
}