using System.ComponentModel.DataAnnotations;

namespace Glinter.Modules.Stays.Application.Listings.Dtos;

public class GetStaysRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Adm3Gid must be greater than zero.")]
    public int? Adm3Gid { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Guests must be greater than zero.")]
    public int? Guests { get; set; }

    [StringLength(100, ErrorMessage = "Tag cannot exceed 100 characters.")]
    public string? Tag { get; set; }

    [Range(1, 10000, ErrorMessage = "Page must be between 1 and 10000.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;
}
