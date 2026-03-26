namespace MyFO.Domain.Exceptions;

/// <summary>
/// Thrown when the current user does not have permission for the requested operation.
/// Converted to HTTP 403 (Forbidden) by the API exception middleware.
/// </summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base("FORBIDDEN", message)
    {
    }
}
