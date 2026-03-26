namespace MyFO.Domain.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict is detected.
/// Converted to HTTP 409 (Conflict) by the API exception middleware.
/// </summary>
public class ConflictException : Exception
{
    public string Code { get; } = "CONFLICT";

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
