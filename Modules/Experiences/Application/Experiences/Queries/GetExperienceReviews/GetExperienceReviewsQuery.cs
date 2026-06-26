namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceReviews;

public class GetExperienceReviewsQuery
{
    public Guid ExperienceId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
