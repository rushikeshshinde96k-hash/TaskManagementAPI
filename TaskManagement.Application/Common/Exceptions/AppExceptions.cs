namespace TaskManagement.Application.Common.Exceptions;

/// <summary>Base type for expected, client-facing errors. Mapped to HTTP responses by the API layer.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string entity, object key) : base($"{entity} with id '{key}' was not found.") { }
}

public sealed class BadRequestException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public BadRequestException(string message, IDictionary<string, string[]>? errors = null) : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.") : base(message) { }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Authentication is required.") : base(message) { }
}
