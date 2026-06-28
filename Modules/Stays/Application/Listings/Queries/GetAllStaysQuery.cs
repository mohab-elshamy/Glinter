namespace Glinter.Modules.Stays.Application.Listings.Queries;

public class GetAllStaysQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPage = 10000;
    public const int MaxPageSize = 100;

    public int? Adm3Gid { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Guests { get; set; }
    public string? Tag { get; set; }
    public string? Search { get; set; }
    public string? Currency { get; set; }
    public DateOnly? CheckInDate { get; set; }
    public DateOnly? CheckOutDate { get; set; }
    public string? SortBy { get; set; }
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;

    public int NormalizedPage => Page <= 0 ? DefaultPage : Math.Min(Page, MaxPage);

    public int NormalizedPageSize => PageSize <= 0
        ? DefaultPageSize
        : Math.Min(PageSize, MaxPageSize);
}
