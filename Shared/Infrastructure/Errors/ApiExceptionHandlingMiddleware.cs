using Microsoft.AspNetCore.Mvc;

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
        var mapping = ApiExceptionMapper.Map(
            exception,
            _environment.IsDevelopment());

        var problemDetails = new ProblemDetails
        {
            Status = mapping.Status,
            Title = mapping.Title,
            Detail = mapping.Detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["errorCode"] = mapping.ErrorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ValidationException { Errors: not null } validationException)
            problemDetails.Extensions["errors"] = validationException.Errors;

        return problemDetails;
    }
}
