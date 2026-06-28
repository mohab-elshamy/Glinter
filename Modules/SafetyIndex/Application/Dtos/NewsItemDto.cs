namespace Glinter.Modules.SafetyIndex.Application.Dtos;

public class NewsItemDto
{
    public string Title { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string Guid { get; set; } = string.Empty;

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public string Description { get; set; } = string.Empty;

    public string SourceName { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public DateTimeOffset FetchedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
