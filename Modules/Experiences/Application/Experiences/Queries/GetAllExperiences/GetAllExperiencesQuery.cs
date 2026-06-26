namespace Glinter.Modules.Experiences.Application.Experiences.Queries.GetAllExperiences;

public class GetAllExperiencesQuery
{
    public int? Adm3Gid { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? Guests { get; set; }

    public Guid? VibeId { get; set; }

    public string? Tag { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
