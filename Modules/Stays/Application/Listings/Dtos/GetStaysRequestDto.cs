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

    [StringLength(200, ErrorMessage = "Search cannot exceed 200 characters.")]
    public string? Search { get; set; }

    [StringLength(10, ErrorMessage = "Currency cannot exceed 10 characters.")]
    public string? Currency { get; set; }

    public DateOnly? CheckInDate { get; set; }

    public DateOnly? CheckOutDate { get; set; }

    [StringLength(20, ErrorMessage = "SortBy cannot exceed 20 characters.")]
    public string? SortBy { get; set; }

    [Range(1, 10000, ErrorMessage = "Page must be between 1 and 10000.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;
}
