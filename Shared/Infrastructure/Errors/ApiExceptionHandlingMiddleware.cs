using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Glinter.Shared.Infrastructure.Errors;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var problemDetails = CreateProblemDetails(context, exception);
        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {StatusCode} while processing {Method} {Path}.",
                statusCode,
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Bad request.",
                exception.Message),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                exception.Message),
            UnauthorizedAccessException => MapUnauthorizedAccess(exception),
            DbUpdateException dbUpdateException => MapDatabaseUpdateException(dbUpdateException),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Invalid operation.",
                exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                _environment.IsDevelopment()
                    ? exception.ToString()
                    : "An unexpected error occurred while processing the request.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        return problemDetails;
    }

    private static (int Status, string Title, string Detail) MapUnauthorizedAccess(Exception exception)
    {
        var message = exception.Message;
        var isAuthenticationFailure =
            message.Contains("authenticated", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("token", StringComparison.OrdinalIgnoreCase);

        return isAuthenticationFailure
            ? (StatusCodes.Status401Unauthorized, "Unauthorized.", message)
            : (StatusCodes.Status403Forbidden, "Forbidden.", message);
    }

    private static (int Status, string Title, string Detail) MapDatabaseUpdateException(
        DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return (
                StatusCodes.Status500InternalServerError,
                "Database update failed.",
                "An unexpected database error occurred while processing the request.");
        }

        return postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => (
                StatusCodes.Status409Conflict,
                "Duplicate resource.",
                "A resource with the same unique value already exists."),
            PostgresErrorCodes.ForeignKeyViolation => (
                StatusCodes.Status400BadRequest,
                "Invalid reference.",
                "One or more referenced resources do not exist."),
            PostgresErrorCodes.StringDataRightTruncation => (
                StatusCodes.Status400BadRequest,
                "Invalid request value.",
                "One or more values exceed the allowed length."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Database update failed.",
                "An unexpected database error occurred while processing the request.")
        };
    }
}
