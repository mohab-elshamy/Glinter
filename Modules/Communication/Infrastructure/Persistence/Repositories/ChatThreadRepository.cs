using Glinter.Modules.Communication.Application.Abstractions;
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

    public async Task<ChatThread?> GetDirectThreadAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default)
    {
        var userIds = new[] { firstUserId, secondUserId };

        return await _dbContext.ChatThreads
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .Where(x => x.Type == ChatThreadType.Direct)
            .Where(x => x.Participants.Count(p => p.LeftAtUtc == null) == 2)
            .Where(x => x.Participants.Count(p => p.LeftAtUtc == null && userIds.Contains(p.UserId)) == 2)
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

    public async Task<ChatThread?> GetByIdWithParticipantsAndMessagesAsync(
        Guid threadId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatThreads
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == threadId, cancellationToken);
    }

    public async Task<List<ChatThread>> GetThreadsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatThreads
            .Include(x => x.Participants)
            .Include(x => x.Messages)
            .Where(x => x.Participants.Any(p => p.UserId == userId && p.LeftAtUtc == null))
            .OrderByDescending(x => x.LastMessageAtUtc ?? x.CreatedAtUtc)
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
}
