using Glinter.Modules.Communication.Application.Notifications.Commands;
using Glinter.Modules.Communication.Application.Notifications.Queries;
using Glinter.Modules.Communication.Application.Notifications.Dtos;
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
    private readonly GetNotificationPreferencesHandler _getPreferencesHandler;
    private readonly UpdateNotificationPreferencesHandler _updatePreferencesHandler;

    public NotificationsController(
        GetMyNotificationsHandler getMyNotificationsHandler,
        MarkNotificationAsReadHandler markNotificationAsReadHandler,
        MarkAllNotificationsAsReadHandler markAllNotificationsAsReadHandler,
        GetNotificationPreferencesHandler getPreferencesHandler,
        UpdateNotificationPreferencesHandler updatePreferencesHandler)
    {
        _getMyNotificationsHandler = getMyNotificationsHandler;
        _markNotificationAsReadHandler = markNotificationAsReadHandler;
        _markAllNotificationsAsReadHandler = markAllNotificationsAsReadHandler;
        _getPreferencesHandler = getPreferencesHandler;
        _updatePreferencesHandler = updatePreferencesHandler;
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

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken) =>
        Ok(await _getPreferencesHandler.HandleAsync(cancellationToken));

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferenceRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await _updatePreferencesHandler.HandleAsync(request, cancellationToken));
}
