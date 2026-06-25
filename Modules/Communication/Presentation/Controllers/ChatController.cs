using Glinter.Modules.Communication.Application.Chats.Commands;
using Glinter.Modules.Communication.Application.Chats.Dtos;
using Glinter.Modules.Communication.Application.Chats.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetMyThreads(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getMyChatThreadsHandler.HandleAsync(
                new GetMyChatThreadsQuery(),
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("threads/direct")]
    public async Task<IActionResult> CreateDirectThread(
        [FromBody] CreateDirectChatThreadRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _createDirectThreadHandler.HandleAsync(
                new CreateDirectChatThreadCommand
                {
                    OtherUserId = request.OtherUserId
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid threadId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getMessagesHandler.HandleAsync(
                new GetChatThreadMessagesQuery
                {
                    ThreadId = threadId,
                    Page = page <= 0 ? 1 : page,
                    PageSize = pageSize <= 0 ? 50 : pageSize
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid threadId,
        [FromBody] SendChatMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPatch("threads/{threadId:guid}/read")]
    public async Task<IActionResult> MarkThreadAsRead(
        Guid threadId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _markThreadAsReadHandler.HandleAsync(
                new MarkChatThreadAsReadCommand
                {
                    ThreadId = threadId
                },
                cancellationToken);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
