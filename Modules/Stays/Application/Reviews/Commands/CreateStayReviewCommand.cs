namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class CreateStayReviewCommand
{
    public Guid StayId { get; set; }
    public Guid TravelerProfileId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}