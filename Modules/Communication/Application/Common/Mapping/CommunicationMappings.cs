using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
using Glinter.Modules.Communication.Domain.Entities;

namespace Glinter.Modules.Communication.Application.Common.Mapping;

public static class CommunicationMappings
{
    public static ChatThreadSummaryDto ToThreadSummaryDto(ChatThread thread, Guid currentUserId)
    {
        var currentParticipant = thread.Participants
            .FirstOrDefault(x => x.UserId == currentUserId);

        var lastMessage = thread.Messages
            .OrderByDescending(x => x.SentAtUtc)
            .FirstOrDefault();

        var unreadCount = thread.Messages.Count(x =>
            x.SenderUserId != currentUserId &&
            (currentParticipant?.LastReadAtUtc is null || x.SentAtUtc > currentParticipant.LastReadAtUtc));

        return new ChatThreadSummaryDto
        {
            Id = thread.Id,
            Type = thread.Type,
            Title = thread.Title,
            ParticipantUserIds = thread.Participants
                .Where(x => x.LeftAtUtc == null)
                .Select(x => x.UserId)
                .OrderBy(x => x)
                .ToList(),
            LastMessageBody = lastMessage?.Body,
            LastMessageSenderUserId = lastMessage?.SenderUserId,
            LastMessageAtUtc = thread.LastMessageAtUtc,
            UnreadCount = unreadCount,
            CreatedAtUtc = thread.CreatedAtUtc
        };
    }

    public static ChatMessageResponseDto ToMessageResponseDto(ChatMessage message, Guid currentUserId)
    {
        return new ChatMessageResponseDto
        {
            Id = message.Id,
            ThreadId = message.ThreadId,
            SenderUserId = message.SenderUserId,
            Body = message.Body,
            SentAtUtc = message.SentAtUtc,
            IsMine = message.SenderUserId == currentUserId
        };
    }

    public static NotificationResponseDto ToNotificationResponseDto(Notification notification)
    {
        return new NotificationResponseDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            LinkUrl = notification.LinkUrl,
            SourceModule = notification.SourceModule,
            SourceEntityType = notification.SourceEntityType,
            SourceEntityId = notification.SourceEntityId,
            CreatedAtUtc = notification.CreatedAtUtc,
            ReadAtUtc = notification.ReadAtUtc,
            IsRead = notification.ReadAtUtc is not null
        };
    }
}
