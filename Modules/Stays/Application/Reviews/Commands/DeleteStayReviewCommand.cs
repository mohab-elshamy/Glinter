namespace Glinter.Modules.Stays.Application.Reviews.Commands;

public class DeleteStayReviewCommand
{
    public Guid ReviewId { get; set; }
    public Guid TravelerProfileId { get; set; }
}