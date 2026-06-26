namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceAvailability;

public class GetExperienceAvailabilityQuery
{
    public Guid ExperienceId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
