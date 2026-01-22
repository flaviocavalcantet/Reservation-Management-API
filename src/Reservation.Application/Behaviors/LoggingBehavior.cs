using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Reservation.Application.Behaviors;

/// <summary>
/// Pipeline behavior for MediatR that provides comprehensive request/response logging.
/// Logs request parameters, execution time, and responses with structured logging.
/// 
/// Pattern: Middleware/Interceptor pattern for cross-cutting concerns
/// - Measures execution time for performance monitoring
/// - Logs at appropriate levels (Debug/Info based on success)
/// - Captures exception details for debugging
/// - Uses structured logging with named parameters
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the LoggingBehavior class.
    /// </summary>
    /// <param name="logger">Logger instance for request/response logging</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs request details, execution time, and response/error information.
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;
        var responseTypeName = typeof(TResponse).Name;

        _logger.LogDebug(
            "Processing request {RequestType} -> {ResponseType}",
            requestTypeName,
            responseTypeName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Request {RequestType} completed successfully in {ElapsedMilliseconds}ms",
                requestTypeName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            _logger.LogError(
                exception,
                "Request {RequestType} failed after {ElapsedMilliseconds}ms. Exception: {ExceptionMessage}",
                requestTypeName,
                stopwatch.ElapsedMilliseconds,
                exception.Message);

            throw;
        }
    }
}
