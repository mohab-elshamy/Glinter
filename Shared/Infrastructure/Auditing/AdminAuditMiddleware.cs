using System.Security.Claims;
using Glinter.Modules.IdentityAccess.Domain.Constants;
using Glinter.Shared.Application.Auditing;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Glinter.Shared.Infrastructure.Auditing;

public sealed class AdminAuditMiddleware
{
    private static readonly HashSet<string> AuditedMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Post,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete
        };

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminAuditMiddleware> _logger;

    public AdminAuditMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<AdminAuditMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AdminAuditDetailsContext auditDetails)
    {
        if (!ShouldAudit(context, out var actorUserId))
        {
            await _next(context);
            return;
        }

        var descriptor = context.GetEndpoint()?.Metadata
            .GetMetadata<ControllerActionDescriptor>();
        var auditEvent = new AdminAuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Action = descriptor is null
                ? $"{context.Request.Method} {context.Request.Path}"
                : $"{descriptor.ControllerName}.{descriptor.ActionName}",
            HttpMethod = context.Request.Method,
            Path = context.Request.Path.Value ?? string.Empty,
            Target = BuildTarget(context),
            StatusCode = StatusCodes.Status102Processing,
            Succeeded = false,
            CorrelationId = context.TraceIdentifier,
            CreatedAtUtc = DateTime.UtcNow
        };

        await RecordIntentAsync(auditEvent);

        Exception? endpointException = null;
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            endpointException = exception;
            throw;
        }
        finally
        {
            try
            {
                var statusCode = endpointException is null
                    ? context.Response.StatusCode
                    : StatusCodes.Status500InternalServerError;

                await CompleteIntentAsync(
                    auditEvent.Id,
                    statusCode,
                    endpointException is null && statusCode is >= 200 and < 400,
                    auditDetails.ChangeDetailsJson);
            }
            catch (Exception auditException)
            {
                _logger.LogError(
                    auditException,
                    "Failed to persist administrative audit event for {Method} {Path}.",
                    context.Request.Method,
                    context.Request.Path);
            }
        }
    }

    private async Task RecordIntentAsync(AdminAuditEvent auditEvent)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var auditService = scope.ServiceProvider.GetRequiredService<AdminAuditService>();
        await auditService.RecordAsync(auditEvent, CancellationToken.None);
    }

    private async Task CompleteIntentAsync(
        Guid auditEventId,
        int statusCode,
        bool succeeded,
        string? changeDetailsJson)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var auditService = scope.ServiceProvider.GetRequiredService<AdminAuditService>();
        await auditService.CompleteAsync(
            auditEventId,
            statusCode,
            succeeded,
            changeDetailsJson,
            CancellationToken.None);
    }

    private static bool ShouldAudit(HttpContext context, out Guid actorUserId)
    {
        actorUserId = Guid.Empty;
        if (!AuditedMethods.Contains(context.Request.Method) ||
            context.User.Identity?.IsAuthenticated != true ||
            !context.User.IsInRole(RoleNames.Admin))
        {
            return false;
        }

        return Guid.TryParse(
            context.User.FindFirstValue(ClaimTypes.NameIdentifier),
            out actorUserId);
    }

    private static string? BuildTarget(HttpContext context)
    {
        var routeValues = context.Request.RouteValues
            .Where(x => x.Key is not "controller" and not "action" &&
                        x.Value is not null)
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key}={x.Value}")
            .ToArray();
        return routeValues.Length == 0 ? null : string.Join(";", routeValues);
    }
}
