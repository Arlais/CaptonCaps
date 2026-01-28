using System.Diagnostics;

namespace CartonCaps.Api.Middleware;

/// <summary>
/// Middleware that adds correlation IDs to requests for distributed tracing.
/// Ensures every request can be tracked through logs and across services.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get correlation ID from request header, or generate a new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

        // Add to Activity for distributed tracing
        Activity.Current?.SetTag("correlation_id", correlationId);

        // Add to logger scope so all logs include the correlation ID
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension methods for registering the correlation ID middleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
