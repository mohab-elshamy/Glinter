using System.Security.Claims;
using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly IIdentityUserReadService _userReadService;
    private readonly ChatConnectionRegistry _connectionRegistry;

    public ChatHub(
        IChatThreadRepository threadRepository,
        IIdentityUserReadService userReadService,
        ChatConnectionRegistry connectionRegistry)
    {
        _threadRepository = threadRepository;
        _userReadService = userReadService;
        _connectionRegistry = connectionRegistry;
    }

    public async Task JoinThread(Guid threadId)
    {
        if (threadId == Guid.Empty)
            throw new HubException("A valid thread id is required.");

        var userId = GetUserId();
        if (!await _userReadService.IsActiveUserAsync(
                userId,
                Context.ConnectionAborted))
        {
            throw new HubException("Your account is inactive.");
        }

        var thread = await _threadRepository.GetByIdWithParticipantsAsync(
            threadId,
            Context.ConnectionAborted);

        if (thread is null)
            throw new HubException("Chat thread was not found.");

        if (!thread.Participants.Any(x => x.UserId == userId && x.LeftAtUtc == null))
            throw new HubException("You are not a participant in this chat thread.");

        _connectionRegistry.Join(threadId, Context.ConnectionId, userId);
    }

    public Task LeaveThread(Guid threadId)
    {
        _connectionRegistry.Leave(threadId, Context.ConnectionId);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionRegistry.RemoveConnection(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
            throw new HubException("Authentication is required.");
        return userId;
    }
}
