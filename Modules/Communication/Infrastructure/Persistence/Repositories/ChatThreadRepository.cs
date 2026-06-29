using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.Communication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Repositories;

public class ChatThreadRepository : IChatThreadRepository
{
    private readonly CommunicationDbContext _dbContext;

    public ChatThreadRepository(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChatThreadSummaryDto?> GetDirectThreadSummaryAsync(
        string directKey,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        return await ProjectSummaries(
                _dbContext.ChatThreads
                    .AsNoTracking()
                    .Where(x =>
                        x.Type == ChatThreadType.Direct &&
                        x.DirectKey == directKey),
                currentUserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ChatThread?> GetByIdWithParticipantsAsync(
        Guid threadId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatThreads
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == threadId, cancellationToken);
    }

    public async Task<List<ChatThreadSummaryDto>> GetThreadSummariesForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page <= 0 ? 1 : Math.Min(page, 10000);
        var normalizedPageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _dbContext.ChatThreads
            .AsNoTracking()
            .Where(x => x.Participants.Any(
                p => p.UserId == userId && p.LeftAtUtc == null))
            .OrderByDescending(x => x.LastMessageAtUtc ?? x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize);

        return await ProjectSummaries(query, userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatParticipant?> GetParticipantForUpdateAsync(
        Guid threadId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatParticipants
            .FirstOrDefaultAsync(
                x => x.ThreadId == threadId &&
                     x.UserId == userId &&
                     x.LeftAtUtc == null,
                cancellationToken);
    }

    public async Task<ChatThread> AddAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ChatThreads.AddAsync(thread, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return thread;
    }

    public async Task<ChatThread?> TryAddDirectThreadAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ChatThreads.AddAsync(thread, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return thread;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            foreach (var entry in ex.Entries)
            {
                entry.State = EntityState.Detached;
            }

            return null;
        }
    }

    public async Task UpdateAsync(
        ChatThread thread,
        CancellationToken cancellationToken = default)
    {
        _dbContext.ChatThreads.Update(thread);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateParticipantAsync(
        ChatParticipant participant,
        CancellationToken cancellationToken = default)
    {
        _dbContext.ChatParticipants.Update(participant);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static IQueryable<ChatThreadSummaryDto> ProjectSummaries(
        IQueryable<ChatThread> query,
        Guid currentUserId)
    {
        return query.Select(thread => new ChatThreadSummaryDto
        {
            Id = thread.Id,
            Type = thread.Type,
            Title = thread.Title,
            ParticipantUserIds = thread.Participants
                .Where(participant => participant.LeftAtUtc == null)
                .OrderBy(participant => participant.UserId)
                .Select(participant => participant.UserId)
                .ToList(),
            LastMessageBody = thread.Messages
                .OrderByDescending(message => message.SentAtUtc)
                .ThenByDescending(message => message.Id)
                .Select(message => message.Body)
                .FirstOrDefault(),
            LastMessageSenderUserId = thread.Messages
                .OrderByDescending(message => message.SentAtUtc)
                .ThenByDescending(message => message.Id)
                .Select(message => (Guid?)message.SenderUserId)
                .FirstOrDefault(),
            LastMessageAtUtc = thread.Messages
                .OrderByDescending(message => message.SentAtUtc)
                .ThenByDescending(message => message.Id)
                .Select(message => (DateTime?)message.SentAtUtc)
                .FirstOrDefault(),
            UnreadCount = thread.Messages.Count(message =>
                message.SenderUserId != currentUserId &&
                !thread.Participants.Any(participant =>
                    participant.UserId == currentUserId &&
                    participant.LeftAtUtc == null &&
                    participant.LastReadAtUtc != null &&
                    message.SentAtUtc <= participant.LastReadAtUtc.Value)),
            CreatedAtUtc = thread.CreatedAtUtc
        });
    }
}
