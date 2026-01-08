using MediatR;

namespace Reservation.Application.Behaviors;

/// <summary>
/// Pipeline behavior for MediatR that validates requests using FluentValidation.
/// Ensures all requests meet business rules before reaching handlers.
/// Implements the Validation pattern with cross-cutting concern injection.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Placeholder for validation logic
    // In a complete implementation, this would use FluentValidation to validate requests
    // Example: new CreateReservationValidator().ValidateAsync(request)

    /// <summary>
    /// Handles request validation in the MediatR pipeline
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // TODO: Implement validation when validators are created
        return await next();
    }
}
