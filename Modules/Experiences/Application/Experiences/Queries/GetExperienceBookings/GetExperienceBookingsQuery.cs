namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetExperienceBookings;

public class GetExperienceBookingsQuery
{
    public Guid ExperienceId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
