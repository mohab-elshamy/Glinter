namespace Glinter.Modules.Stays.Application.Reviews.Dtos;

public class CreateStayReviewRequestDto
{
    public Guid TravelerProfileId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}