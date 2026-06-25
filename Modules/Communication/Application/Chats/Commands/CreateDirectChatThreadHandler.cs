using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.Communication.Domain.Enums;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Chats.Commands;

public class CreateDirectChatThreadHandler
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateDirectChatThreadHandler(
        IChatThreadRepository threadRepository,
        ICurrentUserService currentUserService)
    {
        _threadRepository = threadRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ChatThreadSummaryDto> HandleAsync(
        CreateDirectChatThreadCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (command.OtherUserId == Guid.Empty)
            throw new ArgumentException("OtherUserId is required.");

        if (command.OtherUserId == currentUserId)
            throw new ArgumentException("Cannot create a direct chat with yourself.");

        var existingThread = await _threadRepository.GetDirectThreadAsync(
            currentUserId,
            command.OtherUserId,
            cancellationToken);

        if (existingThread is not null)
            return CommunicationMappings.ToThreadSummaryDto(existingThread, currentUserId);

        var now = DateTime.UtcNow;
        var thread = new ChatThread
        {
            Id = Guid.NewGuid(),
            Type = ChatThreadType.Direct,
            CreatedByUserId = currentUserId,
            CreatedAtUtc = now,
            Participants =
            [
                new ChatParticipant
                {
                    UserId = currentUserId,
                    JoinedAtUtc = now
                },
                new ChatParticipant
                {
                    UserId = command.OtherUserId,
                    JoinedAtUtc = now
                }
            ]
        };

        var createdThread = await _threadRepository.AddAsync(thread, cancellationToken);

        return CommunicationMappings.ToThreadSummaryDto(createdThread, currentUserId);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
