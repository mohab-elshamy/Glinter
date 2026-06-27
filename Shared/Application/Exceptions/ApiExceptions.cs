namespace Glinter.Shared.Application.Exceptions;

public abstract class ApiException : Exception
{
    protected ApiException(string message, string errorCode, Exception? innerException = null)
        : base(message, innerException) => ErrorCode = errorCode;

    public string ErrorCode { get; }
}

public sealed class ValidationException : ApiException
{
    public ValidationException(string message) : base(message, "validation_error") { }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message, "validation_error") => Errors = errors;

    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}

public sealed class AuthenticationException : ApiException
{
    public AuthenticationException(string message = "Authentication is required.")
        : base(message, "authentication_required") { }
}

public sealed class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "You are not allowed to perform this operation.")
        : base(message, "forbidden") { }
}

public sealed class NotFoundException : ApiException
{
    public NotFoundException(string message) : base(message, "not_found") { }
}

public sealed class ConflictException : ApiException
{
    public ConflictException(string message) : base(message, "conflict") { }
}
