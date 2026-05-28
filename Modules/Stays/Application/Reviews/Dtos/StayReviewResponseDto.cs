namespace Glinter.Modules.Stays.Application.Reviews.Dtos;

public class StayReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid StayId { get; set; }
    public Guid TravelerProfileId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}