namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class UpdateStayReviewCommand
{
    public Guid ReviewId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
