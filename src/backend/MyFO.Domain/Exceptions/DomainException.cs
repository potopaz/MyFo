namespace MyFO.Domain.Exceptions;

/// <summary>
/// Base exception for business rule violations.
/// Converted to HTTP 400 (Bad Request) by the API exception middleware.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
