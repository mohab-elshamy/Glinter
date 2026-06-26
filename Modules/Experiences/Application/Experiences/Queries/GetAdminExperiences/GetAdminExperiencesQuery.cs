using Glinter.Modules.Experiences.Domain.Enums;

namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetAdminExperiences;

public class GetAdminExperiencesQuery
{
    public ExperienceApprovalStatus? ApprovalStatus { get; set; }

    public bool? IsActive { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
