using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Shared.Application.Auditing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Glinter.Shared.Infrastructure.Auditing;

public sealed class AdminAuditService
{
    private readonly IdentityAccessDbContext _dbContext;

    public AdminAuditService(IdentityAccessDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RecordAsync(
        AdminAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AdminAuditEvents.AddAsync(auditEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        Guid auditEventId,
        int statusCode,
        bool succeeded,
        string? changeDetailsJson,
        CancellationToken cancellationToken = default)
    {
        var updated = await _dbContext.AdminAuditEvents
            .Where(x => x.Id == auditEventId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.StatusCode, statusCode)
                    .SetProperty(x => x.Succeeded, succeeded)
                    .SetProperty(x => x.ChangeDetailsJson, changeDetailsJson)
                    .SetProperty(x => x.CompletedAtUtc, DateTime.UtcNow),
                cancellationToken);

        if (updated != 1)
            throw new InvalidOperationException("Administrative audit intent was not found.");
    }

    public async Task<AdminAuditPageDto> GetAsync(
        Guid? actorUserId,
        string? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedFromUtc = NormalizeUtc(fromUtc, nameof(fromUtc));
        var normalizedToUtc = NormalizeUtc(toUtc, nameof(toUtc));

        if (normalizedFromUtc.HasValue &&
            normalizedToUtc.HasValue &&
            normalizedFromUtc > normalizedToUtc)
        {
            throw new ValidationException("FromUtc must be on or before ToUtc.");
        }

        var normalizedPage = Math.Clamp(page, 1, 10000);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var query = _dbContext.AdminAuditEvents.AsNoTracking();

        if (actorUserId.HasValue)
            query = query.Where(x => x.ActorUserId == actorUserId.Value);
        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = action.Trim();
            query = query.Where(x => x.Action == normalizedAction);
        }
        if (normalizedFromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= normalizedFromUtc.Value);
        if (normalizedToUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= normalizedToUtc.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var events = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new AdminAuditPageDto
        {
            Items = events
                .Select(x => new AdminAuditEventDto
                {
                    Id = x.Id,
                    ActorUserId = x.ActorUserId,
                    Action = x.Action,
                    HttpMethod = x.HttpMethod,
                    Path = x.Path,
                    Target = x.Target,
                    StatusCode = x.StatusCode,
                    Succeeded = x.Succeeded,
                    CorrelationId = x.CorrelationId,
                    CreatedAtUtc = x.CreatedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    Changes = ParseChanges(x.ChangeDetailsJson)
                })
                .ToList(),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    private static DateTime? NormalizeUtc(DateTime? value, string fieldName)
    {
        if (!value.HasValue)
            return null;
        if (value.Value.Kind == DateTimeKind.Unspecified)
        {
            throw new ValidationException(
                $"{fieldName} must include Z or an explicit UTC offset.");
        }

        return value.Value.ToUniversalTime();
    }

    private static JsonElement? ParseChanges(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return JsonSerializer.Deserialize<JsonElement>(value);
    }
}
