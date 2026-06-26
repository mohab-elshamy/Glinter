namespace Glinter.Modules.Experiences.Application.Experiences.Dtos;

public class GetAdminExperiencesRequestDto
{
    [System.ComponentModel.DataAnnotations.StringLength(30)]
    public string? ApprovalStatus { get; set; }

    public bool? IsActive { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 10000)]
    public int Page { get; set; } = 1;

    [System.ComponentModel.DataAnnotations.Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
