using System.Security.Claims;
using Glinter.Modules.Communication.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatThreadRepository _threadRepository;

    public ChatHub(IChatThreadRepository threadRepository)
    {
        _threadRepository = threadRepository;
    }

    public async Task JoinThread(Guid threadId)
    {
        if (threadId == Guid.Empty)
            throw new HubException("A valid thread id is required.");

        var userId = GetUserId();
        var thread = await _threadRepository.GetByIdWithParticipantsAsync(
            threadId,
            Context.ConnectionAborted);

        if (thread is null)
            throw new HubException("Chat thread was not found.");

        if (!thread.Participants.Any(x => x.UserId == userId && x.LeftAtUtc == null))
            throw new HubException("You are not a participant in this chat thread.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ChatHubGroups.Thread(threadId),
            Context.ConnectionAborted);
    }

    public Task LeaveThread(Guid threadId) =>
        Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ChatHubGroups.Thread(threadId),
            Context.ConnectionAborted);

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
            throw new HubException("Authentication is required.");
        return userId;
    }
}

internal static class ChatHubGroups
{
    public static string Thread(Guid threadId) => $"chat-thread:{threadId:D}";
}
