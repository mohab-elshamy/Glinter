using System.Text.Json;

namespace Glinter.Shared.Application.Auditing;

public sealed class AdminAuditEvent
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Target { get; set; }
    public int StatusCode { get; set; }
    public bool Succeeded { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ChangeDetailsJson { get; set; }
}

public sealed class AdminAuditEventDto
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Target { get; set; }
    public int StatusCode { get; set; }
    public bool Succeeded { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public JsonElement? Changes { get; set; }
}

public sealed class AdminAuditPageDto
{
    public List<AdminAuditEventDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
