using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace CartonCaps.Api.Middleware;

/// <summary>
/// Global exception handler that provides consistent error responses across the API.
/// Implements IExceptionHandler for ASP.NET Core exception handling pipeline.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unhandled exception occurred. Path: {Path}, Method: {Method}",
            httpContext.Request.Path,
            httpContext.Request.Method);

        var (statusCode, errorResponse) = exception switch
        {
            ArgumentNullException => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Invalid request", "A required parameter was null or empty.")
            ),
            ArgumentException => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Invalid request", exception.Message)
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new ErrorResponse("Unauthorized", "You are not authorized to perform this action.")
            ),
            InvalidOperationException => (
                HttpStatusCode.Conflict,
                new ErrorResponse("Operation failed", exception.Message)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("Internal server error", "An unexpected error occurred. Please try again later.")
            )
        };

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(
            errorResponse,
            cancellationToken: cancellationToken);

        return true;
    }
}

/// <summary>
/// Represents a standardized error response.
/// </summary>
public record ErrorResponse(string Title, string Detail)
{
    /// <summary>
    /// The timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// A unique identifier for this error instance (for tracking/support).
    /// </summary>
    public string TraceId { get; init; } = Guid.NewGuid().ToString();
}
