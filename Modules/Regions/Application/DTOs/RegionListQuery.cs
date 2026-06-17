using System.ComponentModel.DataAnnotations;

namespace Glinter.Modules.Regions.Application.DTOs;

/// <summary>Shared pagination / search query parameters for all region list endpoints.</summary>
public class RegionListQuery
{
    /// <summary>Filter by name (English or Arabic) or pcode — partial match.</summary>
    public string? Search { get; set; }

    /// <summary>0 omits geometry, 100 returns full geometry, 1-99 returns simplified geometry.</summary>
    [Range(0, 100, ErrorMessage = "GeometryAccuracy must be between 0 and 100.")]
    public int GeometryAccuracy { get; set; } = 0;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
