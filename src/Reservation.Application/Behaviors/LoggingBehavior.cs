using MediatR;

namespace Reservation.Application.Behaviors;

/// <summary>
/// Pipeline behavior for MediatR that provides request/response logging.
/// Useful for debugging, monitoring, and understanding request flow through the application.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>
    /// Handles the request with logging before and after processing
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        // Log request details (in production, use ILogger)
        System.Diagnostics.Debug.WriteLine($"[Request] {requestName}");

        try
        {
            var response = await next();
            
            System.Diagnostics.Debug.WriteLine($"[Response] {requestName}");
            return response;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Error] {requestName}: {ex.Message}");
            throw;
        }
    }
}
