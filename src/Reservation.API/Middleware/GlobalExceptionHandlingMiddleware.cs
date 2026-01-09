using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Reservation.API.Middleware;

/// <summary>
/// Global exception handling middleware that captures unhandled exceptions,
/// logs them with structured logging, and returns standardized error responses.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionHandlingMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger instance for exception handling.</param>
    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions in the request pipeline.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Get correlation ID for tracing
            var correlationId = CorrelationIdMiddleware.GetCorrelationId(context);

            // Log the exception with full context
            LogException(context, ex, correlationId);

            // Return standardized error response
            await HandleExceptionAsync(context, ex, correlationId);
        }
    }

    /// <summary>
    /// Logs the exception with structured logging including context information.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="correlationId">The correlation ID for request tracing.</param>
    private void LogException(HttpContext context, Exception exception, string correlationId)
    {
        var severity = GetExceptionSeverity(exception);

        if (severity == "critical" || severity == "error")
        {
            _logger.LogError(
                exception,
                "Unhandled exception ({ExceptionType}) in {Method} {Path}. " +
                "CorrelationId: {CorrelationId}. RemoteIP: {RemoteIP}. Severity: {Severity}",
                exception.GetType().Name,
                context.Request.Method,
                context.Request.Path,
                correlationId,
                context.Connection.RemoteIpAddress,
                severity);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Validation exception in {Method} {Path}. " +
                "CorrelationId: {CorrelationId}. RemoteIP: {RemoteIP}",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                context.Connection.RemoteIpAddress);
        }
    }

    /// <summary>
    /// Handles the exception by returning a standardized error response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="correlationId">The correlation ID for request tracing.</param>
    /// <returns>A completed task after response is sent.</returns>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errorType, message) = GetErrorDetails(exception);

        context.Response.StatusCode = statusCode;

        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Type = errorType,
                Message = message,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow.ToString("O")
            }
        };

        // Add additional validation error details if applicable
        if (exception is ValidationException validationEx)
        {
            errorResponse.Error.Details = new[] { validationEx.Message };
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(errorResponse, options);

        return context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Determines the HTTP status code, error type, and message based on exception type.
    /// </summary>
    /// <param name="exception">The exception to analyze.</param>
    /// <returns>A tuple containing status code, error type name, and user-friendly message.</returns>
    private static (int StatusCode, string ErrorType, string Message) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (StatusCode: 400, ErrorType: "ArgumentError", Message: "Invalid argument provided."),
            ValidationException => (StatusCode: 400, ErrorType: "ValidationError", Message: "Validation failed."),
            InvalidOperationException => (StatusCode: 409, ErrorType: "ConflictError", Message: "Operation cannot be performed in current state."),
            KeyNotFoundException => (StatusCode: 404, ErrorType: "NotFoundError", Message: "Requested resource not found."),
            UnauthorizedAccessException => (StatusCode: 401, ErrorType: "UnauthorizedError", Message: "Authentication is required."),
            _ => (StatusCode: 500, ErrorType: "InternalServerError", Message: "An unexpected error occurred. Please try again later.")
        };
    }

    /// <summary>
    /// Determines the severity level of an exception.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>A severity level as string: "critical", "error", or "warning".</returns>
    private static string GetExceptionSeverity(Exception exception)
    {
        return exception switch
        {
            ValidationException => "warning",
            ArgumentException => "warning",
            _ => "error"
        };
    }
}

/// <summary>
/// Standardized error response format for API errors.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error detail information.
    /// </summary>
    public ErrorDetail Error { get; set; } = new();
}

/// <summary>
/// Detailed error information including type, message, and correlation ID.
/// </summary>
public class ErrorDetail
{
    /// <summary>
    /// Gets or sets the error type/category.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID for request tracing.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error timestamp in ISO 8601 format.
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error details or field-specific information.
    /// </summary>
    public string[]? Details { get; set; }
}
