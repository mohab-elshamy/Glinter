namespace Glinter.Modules.Stays.Domain.Entities;

public class StayReviewsPerRating
{
    public int Id { get; set; }
    public int StayId { get; set; }
    public int Rating { get; set; }
    public int ReviewsCount { get; set; }

    public Stay Stay { get; set; } = null!;
}
