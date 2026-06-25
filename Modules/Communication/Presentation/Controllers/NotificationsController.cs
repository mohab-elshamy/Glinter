using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Application.Notifications.Queries;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Glinter.Modules.Communication.Presentation.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsController : ControllerBase
{
    private readonly GetMyNotificationsHandler _getMyNotificationsHandler;
    private readonly MarkNotificationAsReadHandler _markNotificationAsReadHandler;
    private readonly MarkAllNotificationsAsReadHandler _markAllNotificationsAsReadHandler;

    public NotificationsController(
        GetMyNotificationsHandler getMyNotificationsHandler,
        MarkNotificationAsReadHandler markNotificationAsReadHandler,
        MarkAllNotificationsAsReadHandler markAllNotificationsAsReadHandler)
    {
        _getMyNotificationsHandler = getMyNotificationsHandler;
        _markNotificationAsReadHandler = markNotificationAsReadHandler;
        _markAllNotificationsAsReadHandler = markAllNotificationsAsReadHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getMyNotificationsHandler.HandleAsync(
                new GetMyNotificationsQuery
                {
                    Page = page <= 0 ? 1 : page,
                    PageSize = pageSize <= 0 ? 50 : pageSize
                },
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _markNotificationAsReadHandler.HandleAsync(
                new MarkNotificationAsReadCommand
                {
                    NotificationId = id
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        try
        {
            await _markAllNotificationsAsReadHandler.HandleAsync(
                new MarkAllNotificationsAsReadCommand(),
                cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
