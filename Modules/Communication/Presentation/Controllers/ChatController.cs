using Glinter.Modules.Communication.Application.Chats.Commands;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Chats.Queries;
using Glinter.Modules.Communication.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Glinter.Modules.Communication.Presentation.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChatController : ControllerBase
{
    private readonly CreateDirectChatThreadHandler _createDirectThreadHandler;
    private readonly GetMyChatThreadsHandler _getMyChatThreadsHandler;
    private readonly GetChatThreadMessagesHandler _getMessagesHandler;
    private readonly SendChatMessageHandler _sendMessageHandler;
    private readonly MarkChatThreadAsReadHandler _markThreadAsReadHandler;

    public ChatController(
        CreateDirectChatThreadHandler createDirectThreadHandler,
        GetMyChatThreadsHandler getMyChatThreadsHandler,
        GetChatThreadMessagesHandler getMessagesHandler,
        SendChatMessageHandler sendMessageHandler,
        MarkChatThreadAsReadHandler markThreadAsReadHandler)
    {
        _createDirectThreadHandler = createDirectThreadHandler;
        _getMyChatThreadsHandler = getMyChatThreadsHandler;
        _getMessagesHandler = getMessagesHandler;
        _sendMessageHandler = sendMessageHandler;
        _markThreadAsReadHandler = markThreadAsReadHandler;
    }

    [HttpGet("threads")]
    public async Task<IActionResult> GetMyThreads(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _getMyChatThreadsHandler.HandleAsync(
            new GetMyChatThreadsQuery
            {
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("threads/direct")]
    [EnableRateLimiting(CommunicationRateLimitPolicies.DirectThreadCreation)]
    public async Task<IActionResult> CreateDirectThread(
        [FromBody] CreateDirectChatThreadRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _createDirectThreadHandler.HandleAsync(
            new CreateDirectChatThreadCommand
            {
                OtherUserId = request.OtherUserId
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid threadId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _getMessagesHandler.HandleAsync(
            new GetChatThreadMessagesQuery
            {
                ThreadId = threadId,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("threads/{threadId:guid}/messages")]
    [EnableRateLimiting(CommunicationRateLimitPolicies.MessageSending)]
    public async Task<IActionResult> SendMessage(
        Guid threadId,
        [FromBody] SendChatMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _sendMessageHandler.HandleAsync(
            new SendChatMessageCommand
            {
                ThreadId = threadId,
                Body = request.Body
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("threads/{threadId:guid}/read")]
    public async Task<IActionResult> MarkThreadAsRead(
        Guid threadId,
        CancellationToken cancellationToken)
    {
        await _markThreadAsReadHandler.HandleAsync(
            new MarkChatThreadAsReadCommand
            {
                ThreadId = threadId
            },
            cancellationToken);

        return NoContent();
    }
}
