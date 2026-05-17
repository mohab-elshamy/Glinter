namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class GetExperiencesRequestDto
{
    public Guid? AreaId { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? Guests { get; set; }

    public Guid? VibeId { get; set; }

    public string? Tag { get; set; }
}