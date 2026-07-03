using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Glinter.Shared.Infrastructure.Errors;

public static class ApiExceptionMapper
{
    public static ApiErrorMapping Map(Exception exception, bool includeExceptionDetails)
    {
        return exception switch
        {
            ValidationException ex =>
                new(400, "Validation failed.", ex.Message, ex.ErrorCode),
            AuthenticationException ex =>
                new(401, "Unauthorized.", ex.Message, ex.ErrorCode),
            ForbiddenException ex =>
                new(403, "Forbidden.", ex.Message, ex.ErrorCode),
            NotFoundException ex =>
                new(404, "Resource not found.", ex.Message, ex.ErrorCode),
            ConflictException ex =>
                new(409, "Conflict.", ex.Message, ex.ErrorCode),
            BadHttpRequestException ex =>
                new(400, "Bad request.", ex.Message, "bad_request"),
            DbUpdateConcurrencyException =>
                new(
                    409,
                    "Concurrency conflict.",
                    "The resource was changed by another request. Reload it and try again.",
                    "concurrency_conflict"),
            DbUpdateException ex => MapDatabaseUpdateException(ex),
            _ => new(
                500,
                "An unexpected error occurred.",
                "The server could not complete the request.",
                "internal_server_error")
        };
    }

    private static ApiErrorMapping MapDatabaseUpdateException(
        DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return new ApiErrorMapping(
                500,
                "Database update failed.",
                "An unexpected database error occurred while processing the request.",
                "database_update_failed");
        }

        return postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new(
                409,
                "Duplicate resource.",
                "A resource with the same unique value already exists.",
                "duplicate_resource"),
            PostgresErrorCodes.ForeignKeyViolation => new(
                400,
                "Invalid reference.",
                "One or more referenced resources do not exist.",
                "invalid_reference"),
            PostgresErrorCodes.StringDataRightTruncation => new(
                400,
                "Invalid request value.",
                "One or more values exceed the allowed length.",
                "value_too_long"),
            PostgresErrorCodes.CheckViolation => new(
                400,
                "Invalid request value.",
                "One or more values violate a database constraint.",
                "constraint_violation"),
            _ => new ApiErrorMapping(
                500,
                "Database update failed.",
                "An unexpected database error occurred while processing the request.",
                "database_update_failed")
        };
    }
}

public sealed record ApiErrorMapping(
    int Status,
    string Title,
    string Detail,
    string ErrorCode);
