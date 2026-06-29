namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class GetExperiencesRequestDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? Adm3Gid { get; set; }

    public Guid? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? Guests { get; set; }

    public Guid? VibeId { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string? Tag { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(200)]
    public string? Search { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(10)]
    public string? Currency { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? MinDurationMinutes { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int? MaxDurationMinutes { get; set; }

    public DateTime? AvailableFromUtc { get; set; }

    public DateTime? AvailableToUtc { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(20)]
    public string? SortBy { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 10000)]
    public int Page { get; set; } = 1;

    [System.ComponentModel.DataAnnotations.Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
