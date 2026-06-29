using System.ComponentModel.DataAnnotations;

namespace Glinter.Modules.Regions.Application.DTOs;

/// <summary>Shared pagination / search query parameters for all region list endpoints.</summary>
public class RegionListQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPage = 10000;
    public const int MaxPageSize = 100;

    /// <summary>Filter by name (English or Arabic) or pcode — partial match.</summary>
    [StringLength(100, ErrorMessage = "Search cannot exceed 100 characters.")]
    public string? Search { get; set; }

    /// <summary>0 omits geometry, 100 returns full geometry, 1-99 returns simplified geometry.</summary>
    [Range(0, 100, ErrorMessage = "GeometryAccuracy must be between 0 and 100.")]
    public int GeometryAccuracy { get; set; } = 0;

    [Range(1, MaxPage, ErrorMessage = "Page must be between 1 and 10000.")]
    public int Page { get; set; } = DefaultPage;

    [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = DefaultPageSize;

    public int NormalizedPage => Page <= 0 ? DefaultPage : Math.Min(Page, MaxPage);

    public int NormalizedPageSize => PageSize <= 0
        ? DefaultPageSize
        : Math.Min(PageSize, MaxPageSize);
}
