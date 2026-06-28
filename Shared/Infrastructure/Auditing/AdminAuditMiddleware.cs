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
    private readonly ILogger<AdminAuditMiddleware> _logger;

    public AdminAuditMiddleware(
        RequestDelegate next,
        ILogger<AdminAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        AdminAuditService auditService)
    {
        if (!ShouldAudit(context, out var actorUserId))
        {
            await _next(context);
            return;
        }

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
                var descriptor = context.GetEndpoint()?.Metadata
                    .GetMetadata<ControllerActionDescriptor>();
                var statusCode = endpointException is null
                    ? context.Response.StatusCode
                    : StatusCodes.Status500InternalServerError;

                await auditService.RecordAsync(
                    new AdminAuditEvent
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = actorUserId,
                        Action = descriptor is null
                            ? $"{context.Request.Method} {context.Request.Path}"
                            : $"{descriptor.ControllerName}.{descriptor.ActionName}",
                        HttpMethod = context.Request.Method,
                        Path = context.Request.Path.Value ?? string.Empty,
                        Target = BuildTarget(context),
                        StatusCode = statusCode,
                        Succeeded = endpointException is null &&
                                    statusCode is >= 200 and < 400,
                        CorrelationId = context.TraceIdentifier,
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    CancellationToken.None);
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
