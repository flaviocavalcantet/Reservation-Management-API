using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentValidation;
using Reservation.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Reservation.API.Middleware;

/// <summary>
/// Global exception handling middleware that captures unhandled exceptions,
/// logs them with structured logging, and returns standardized error responses.
/// 
/// Pattern: Central exception handling with semantic mapping
/// - Handles domain exceptions with business-aware responses
/// - Maps validation errors to 400 Bad Request
/// - Maps not found to 404
/// - Maps conflicts to 409
/// - Defaults to 500 for unexpected errors
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
        var exceptionType = exception.GetType().Name;

        switch (severity)
        {
            case "critical":
                _logger.LogCritical(
                    exception,
                    "Critical system error ({ExceptionType}) in {Method} {Path}. " +
                    "CorrelationId: {CorrelationId}. RemoteIP: {RemoteIP}",
                    exceptionType,
                    context.Request.Method,
                    context.Request.Path,
                    correlationId,
                    context.Connection.RemoteIpAddress);
                break;

            case "error":
                _logger.LogError(
                    exception,
                    "Unhandled exception ({ExceptionType}) in {Method} {Path}. " +
                    "CorrelationId: {CorrelationId}. RemoteIP: {RemoteIP}",
                    exceptionType,
                    context.Request.Method,
                    context.Request.Path,
                    correlationId,
                    context.Connection.RemoteIpAddress);
                break;

            case "warning":
            default:
                _logger.LogWarning(
                    exception,
                    "Business rule violation ({ExceptionType}) in {Method} {Path}. " +
                    "CorrelationId: {CorrelationId}",
                    exceptionType,
                    context.Request.Method,
                    context.Request.Path,
                    correlationId);
                break;
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

        // Add additional details for specific exception types
        AddExceptionDetails(exception, errorResponse.Error);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(errorResponse, options);

        return context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Adds exception-specific details to the error response.
    /// </summary>
    private static void AddExceptionDetails(Exception exception, ErrorDetail errorDetail)
    {
        switch (exception)
        {
            case FluentValidation.ValidationException validationEx:
                errorDetail.Details = new[] { validationEx.Message };
                break;

            case InvalidAggregateStateException stateEx:
                errorDetail.Details = new[]
                {
                    $"Current state: {stateEx.CurrentState}",
                    $"Requested operation: {stateEx.RequestedOperation}"
                };
                break;

            case DomainValidationException domainValidEx:
                errorDetail.Details = domainValidEx.Errors;
                break;

            case BusinessRuleViolationException ruleEx:
                errorDetail.Details = new[] { $"Rule: {ruleEx.RuleName}" };
                break;

            case AggregateNotFoundException notFoundEx:
                errorDetail.Details = new[] { $"{notFoundEx.AggregateType} with ID '{notFoundEx.AggregateId}'" };
                break;
        }
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
            // Domain exceptions - client errors (4xx)
            DomainValidationException => (400, "ValidationError", "One or more validation errors occurred."),
            InvalidAggregateStateException => (409, "ConflictError", "The operation cannot be performed in the current state."),
            AggregateNotFoundException => (404, "NotFoundError", "The requested resource was not found."),
            AggregateConflictException => (409, "ConflictError", "The resource conflicts with an existing resource."),
            BusinessRuleViolationException => (422, "UnprocessableEntity", "The request violates business rules."),

            // Framework exceptions
            FluentValidation.ValidationException => (400, "ValidationError", "Request validation failed."),
            ArgumentException => (400, "ArgumentError", "Invalid argument provided."),
            KeyNotFoundException => (404, "NotFoundError", "Requested resource not found."),
            UnauthorizedAccessException => (401, "UnauthorizedError", "Authentication is required."),

            // Default
            _ => (500, "InternalServerError", "An unexpected error occurred. Please try again later.")
        };
    }

    /// <summary>
    /// Determines the severity level of an exception for logging purposes.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns>A severity level as string: "critical", "error", or "warning".</returns>
    private static string GetExceptionSeverity(Exception exception)
    {
        return exception switch
        {
            // Domain business rule violations - expected, log as warning
            BusinessRuleViolationException or
            InvalidAggregateStateException or
            DomainValidationException or
            AggregateNotFoundException => "warning",

            // Validation errors - expected, log as warning
            FluentValidation.ValidationException or ArgumentException => "warning",

            // Infrastructure/system errors - unexpected
            DomainException => "error",

            // Everything else - unexpected error
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
