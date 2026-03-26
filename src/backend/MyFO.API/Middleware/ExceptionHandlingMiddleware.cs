using System.Text.Json;
using MyFO.Domain.Exceptions;

namespace MyFO.API.Middleware;

/// <summary>
/// Global exception handler that catches any unhandled exception and returns
/// a consistent JSON error response.
///
/// Instead of letting ASP.NET return a generic 500 error (or worse, a stack trace),
/// this middleware maps our custom exceptions to proper HTTP status codes:
///
///   - NotFoundException    → 404 Not Found
///   - ForbiddenException   → 403 Forbidden
///   - DomainException      → 400 Bad Request (business rule violation)
///   - Any other exception  → 500 Internal Server Error
///
/// The response always has the same shape:
///   { "code": "NOT_FOUND", "message": "Movement with key '...' was not found." }
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Code, ex.Message),
            ForbiddenException ex => (StatusCodes.Status403Forbidden, ex.Code, ex.Message),
            ConflictException ex => (StatusCodes.Status409Conflict, ex.Code, ex.Message),
            DomainException ex => (StatusCodes.Status400BadRequest, ex.Code, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        // Log the full exception for debugging (but don't expose internals to the client)
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Code}", code);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = JsonSerializer.Serialize(new { code, message },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(response);
    }
}
