namespace Glinter.Modules.Profiles.Application.Profiles.Queries.GetLocalBuddies;

public class GetLocalBuddiesQuery
{
    public string? City { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}