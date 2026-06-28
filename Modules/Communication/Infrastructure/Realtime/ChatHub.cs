using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Glinter.Modules.Communication.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Application.Abstractions;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Glinter.Modules.Communication.Infrastructure.Realtime;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatThreadRepository _threadRepository;
    private readonly IIdentityUserReadService _userReadService;
    private readonly ITokenRevocationService _tokenRevocationService;
    private readonly ChatConnectionRegistry _connectionRegistry;

    public ChatHub(
        IChatThreadRepository threadRepository,
        IIdentityUserReadService userReadService,
        ITokenRevocationService tokenRevocationService,
        ChatConnectionRegistry connectionRegistry)
    {
        _threadRepository = threadRepository;
        _userReadService = userReadService;
        _tokenRevocationService = tokenRevocationService;
        _connectionRegistry = connectionRegistry;
    }

    public async Task JoinThread(Guid threadId)
    {
        if (threadId == Guid.Empty)
            throw new HubException("A valid thread id is required.");

        var registration = GetRegistration();
        if (registration.ExpiresAtUtc <= DateTime.UtcNow ||
            !await _userReadService.IsActiveUserWithSecurityStampAsync(
                registration.UserId,
                registration.SecurityStamp,
                Context.ConnectionAborted))
        {
            throw new HubException("Your authentication session is no longer valid.");
        }

        if (await _tokenRevocationService.IsRevokedAsync(
                registration.Jti,
                Context.ConnectionAborted))
        {
            throw new HubException("Your authentication session is no longer valid.");
        }

        var thread = await _threadRepository.GetByIdWithParticipantsAsync(
            threadId,
            Context.ConnectionAborted);

        if (thread is null)
            throw new HubException("Chat thread was not found.");

        if (!thread.Participants.Any(
                x => x.UserId == registration.UserId && x.LeftAtUtc == null))
        {
            throw new HubException("You are not a participant in this chat thread.");
        }

        _connectionRegistry.Join(threadId, registration);
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

    private ChatConnectionRegistration GetRegistration()
    {
        var principal = Context.User;
        var userIdValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var jti = principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var securityStamp = principal?.FindFirstValue(ClaimNames.SecurityStamp);
        var expirationValue = principal?.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (!Guid.TryParse(userIdValue, out var userId) ||
            string.IsNullOrWhiteSpace(jti) ||
            string.IsNullOrWhiteSpace(securityStamp) ||
            !long.TryParse(expirationValue, out var expirationSeconds))
        {
            throw new HubException("Authentication is required.");
        }

        return new ChatConnectionRegistration(
            Context.ConnectionId,
            userId,
            jti,
            securityStamp,
            DateTimeOffset.FromUnixTimeSeconds(expirationSeconds).UtcDateTime);
    }
}
