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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request was cancelled by the client while processing {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "An exception occurred after the response started for {Method} {Path}; correlation {CorrelationId}.",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
                throw;
            }

            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var problemDetails = CreateProblemDetails(context, exception);
        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception while processing {Method} {Path}; correlation {CorrelationId}.",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(exception,
                "Request failed with {StatusCode} while processing {Method} {Path}; correlation {CorrelationId}.",
                statusCode,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        var (status, title, detail, errorCode) = exception switch
        {
            ValidationException ex => (400, "Validation failed.", ex.Message, ex.ErrorCode),
            AuthenticationException ex => (401, "Unauthorized.", ex.Message, ex.ErrorCode),
            ForbiddenException ex => (403, "Forbidden.", ex.Message, ex.ErrorCode),
            NotFoundException ex => (404, "Resource not found.", ex.Message, ex.ErrorCode),
            ConflictException ex => (409, "Conflict.", ex.Message, ex.ErrorCode),
            BadHttpRequestException ex => (400, "Bad request.", ex.Message, "bad_request"),
            DbUpdateConcurrencyException => (409, "Concurrency conflict.",
                "The resource was changed by another request. Reload it and try again.",
                "concurrency_conflict"),
            DbUpdateException ex => MapDatabaseUpdateException(ex),
            _ => (500, "An unexpected error occurred.",
                _environment.IsDevelopment()
                    ? exception.ToString()
                    : "An unexpected error occurred while processing the request.",
                "internal_server_error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ValidationException { Errors: not null } validationException)
            problemDetails.Extensions["errors"] = validationException.Errors;

        return problemDetails;
    }

    private static (int Status, string Title, string Detail, string ErrorCode)
        MapDatabaseUpdateException(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
            return (500, "Database update failed.",
                "An unexpected database error occurred while processing the request.",
                "database_update_failed");

        return postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => (409, "Duplicate resource.",
                "A resource with the same unique value already exists.", "duplicate_resource"),
            PostgresErrorCodes.ForeignKeyViolation => (400, "Invalid reference.",
                "One or more referenced resources do not exist.", "invalid_reference"),
            PostgresErrorCodes.StringDataRightTruncation => (400, "Invalid request value.",
                "One or more values exceed the allowed length.", "value_too_long"),
            PostgresErrorCodes.CheckViolation => (400, "Invalid request value.",
                "One or more values violate a database constraint.", "constraint_violation"),
            _ => (500, "Database update failed.",
                "An unexpected database error occurred while processing the request.",
                "database_update_failed")
        };
    }
}
