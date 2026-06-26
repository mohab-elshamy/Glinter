namespace Glinter.Modules.Experiences.Application.Common;

public static class ExperiencePagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPage = 10000;
    public const int MaxPageSize = 100;

    public static void Validate(int page, int pageSize)
    {
        if (page < 1 || page > MaxPage)
            throw new ArgumentException($"Page must be between 1 and {MaxPage}.");

        if (pageSize < 1 || pageSize > MaxPageSize)
            throw new ArgumentException($"PageSize must be between 1 and {MaxPageSize}.");
    }

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        return (
            page <= 0 ? DefaultPage : Math.Min(page, MaxPage),
            pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize));
    }
}
