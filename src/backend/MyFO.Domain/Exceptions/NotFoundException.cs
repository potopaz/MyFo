namespace MyFO.Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist.
/// Converted to HTTP 404 (Not Found) by the API exception middleware.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entityName, object key)
        : base("NOT_FOUND", $"{entityName} with key '{key}' was not found.")
    {
    }
}
