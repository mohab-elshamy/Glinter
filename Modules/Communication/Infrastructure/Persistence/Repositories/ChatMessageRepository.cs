using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Glinter.Modules.Communication.Infrastructure.Persistence.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly CommunicationDbContext _dbContext;

    public ChatMessageRepository(CommunicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ChatMessage> AddAsync(
        ChatMessage message,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ChatMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<ChatMessage> AddMessageAndUpdateThreadAsync(
        ChatMessage message,
        ChatThread thread,
        ChatParticipant participant,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ChatMessages.AddAsync(message, cancellationToken);
        _dbContext.ChatThreads.Update(thread);
        _dbContext.ChatParticipants.Update(participant);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task<List<ChatMessage>> GetByThreadIdAsync(
        Guid threadId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .AsNoTracking()
            .Where(x => x.ThreadId == threadId)
            .OrderByDescending(x => x.SentAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
