using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Common.Mapping;
using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Domain.Entities;
using Glinter.Modules.Communication.Domain.Enums;
using Glinter.Modules.IdentityAccess.Application.Abstractions;

namespace Glinter.Modules.Communication.Application.Chats.Commands;

public class SendChatMessageHandler
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatRealtimeNotifier _realtimeNotifier;
    private readonly CreateNotificationHandler _createNotificationHandler;

    public SendChatMessageHandler(
        IChatThreadRepository threadRepository,
        IChatMessageRepository messageRepository,
        ICurrentUserService currentUserService,
        IChatRealtimeNotifier realtimeNotifier,
        CreateNotificationHandler createNotificationHandler)
    {
        _threadRepository = threadRepository;
        _messageRepository = messageRepository;
        _currentUserService = currentUserService;
        _realtimeNotifier = realtimeNotifier;
        _createNotificationHandler = createNotificationHandler;
    }

    public async Task<ChatMessageResponseDto> HandleAsync(
        SendChatMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = GetCurrentUserId();

        if (command.ThreadId == Guid.Empty)
            throw new ValidationException("ThreadId is required.");

        var body = command.Body?.Trim();
        if (string.IsNullOrWhiteSpace(body))
            throw new ValidationException("Message body is required.");

        if (body.Length > 4000)
            throw new ValidationException("Message body cannot exceed 4000 characters.");

        var thread = await _threadRepository.GetByIdWithParticipantsAsync(
            command.ThreadId,
            cancellationToken);

        if (thread is null)
            throw new NotFoundException("Chat thread was not found.");

        var participant = thread.Participants
            .FirstOrDefault(x => x.UserId == currentUserId && x.LeftAtUtc == null);

        if (participant is null)
            throw new ForbiddenException("You are not a participant in this chat thread.");

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = command.ThreadId,
            SenderUserId = currentUserId,
            Body = body,
            SentAtUtc = now
        };

        thread.LastMessageAtUtc = now;
        participant.LastReadAtUtc = now;

        var createdMessage = await _messageRepository.AddMessageAndUpdateThreadAsync(
            message,
            thread,
            participant,
            cancellationToken);

        await _realtimeNotifier.MessageCreatedAsync(
            new ChatMessageEventDto
            {
                Id = createdMessage.Id,
                ThreadId = createdMessage.ThreadId,
                SenderUserId = createdMessage.SenderUserId,
                Body = createdMessage.Body,
                SentAtUtc = createdMessage.SentAtUtc
            },
            cancellationToken);

        var preview = body.Length <= 180 ? body : $"{body[..177]}...";
        foreach (var recipientUserId in thread.Participants
                     .Where(x => x.LeftAtUtc == null && x.UserId != currentUserId)
                     .Select(x => x.UserId)
                     .Distinct())
        {
            await _createNotificationHandler.HandleAsync(
                new CreateNotificationCommand
                {
                    UserId = recipientUserId,
                    Type = NotificationType.ChatMessage,
                    Title = "New message",
                    Body = preview,
                    LinkUrl = $"/messages?thread={thread.Id}",
                    SourceModule = "Communication",
                    SourceEntityType = "ChatMessage",
                    SourceEntityId = createdMessage.Id
                },
                cancellationToken);
        }

        return CommunicationMappings.ToMessageResponseDto(createdMessage, currentUserId);
    }

    private Guid GetCurrentUserId()
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            throw new AuthenticationException("User is not authenticated.");

        return _currentUserService.UserId.Value;
    }
}
