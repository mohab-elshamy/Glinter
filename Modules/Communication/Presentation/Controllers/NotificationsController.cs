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
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _getMyNotificationsHandler.HandleAsync(
            new GetMyNotificationsQuery
            {
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _markNotificationAsReadHandler.HandleAsync(
            new MarkNotificationAsReadCommand
            {
                NotificationId = id
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _markAllNotificationsAsReadHandler.HandleAsync(
            new MarkAllNotificationsAsReadCommand(),
            cancellationToken);

        return NoContent();
    }
}
