using System.Diagnostics;

namespace Reservation.API.Middleware;

/// <summary>
/// Middleware that manages correlation IDs for distributed tracing across the request pipeline.
/// Extracts X-Correlation-ID from request headers or generates a new one.
/// Stores it in HTTP context for access throughout the request.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the CorrelationIdMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger instance for middleware operations.</param>
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// HTTP request correlation ID header name.
    /// </summary>
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    /// <summary>
    /// HTTP context item key for storing correlation ID.
    /// </summary>
    public const string CorrelationIdContextKey = "CorrelationId";

    /// <summary>
    /// Invokes the middleware to set up correlation ID for the request.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Extract correlation ID from request headers or generate new one
        var correlationId = GetOrGenerateCorrelationId(context);

        // Store in context items for downstream access
        context.Items[CorrelationIdContextKey] = correlationId;

        // Add to response headers for client visibility
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

        // Set Activity ID for distributed tracing integration
        if (Activity.Current == null)
        {
            Activity.Current?.SetTag("correlation-id", correlationId);
        }

        _logger.LogInformation(
            "Correlation ID {CorrelationId} assigned to request {Method} {Path}",
            correlationId,
            context.Request.Method,
            context.Request.Path);

        // Continue pipeline
        await _next(context);
    }

    /// <summary>
    /// Gets existing correlation ID from request headers or generates a new GUID.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID as string.</returns>
    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdValue))
        {
            return correlationIdValue.ToString();
        }

        // Generate new ID if not provided
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Extension method to retrieve correlation ID from HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID, or empty string if not found.</returns>
    public static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(CorrelationIdContextKey, out var correlationId))
        {
            return correlationId?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}
