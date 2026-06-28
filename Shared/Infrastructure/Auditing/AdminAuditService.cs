using Glinter.Modules.IdentityAccess.Infrastructure.Persistence;
using Glinter.Shared.Application.Auditing;
using Microsoft.EntityFrameworkCore;

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

    public async Task<AdminAuditPageDto> GetAsync(
        Guid? actorUserId,
        string? action,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
            throw new ValidationException("FromUtc must be on or before ToUtc.");

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
        if (fromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= toUtc.Value);

        return new AdminAuditPageDto
        {
            Items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .ThenByDescending(x => x.Id)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
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
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(cancellationToken),
            Page = normalizedPage,
            PageSize = normalizedPageSize,
            TotalCount = await query.CountAsync(cancellationToken)
        };
    }
}
