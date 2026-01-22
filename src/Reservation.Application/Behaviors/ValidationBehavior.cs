using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reservation.Application.Behaviors;

/// <summary>
/// Pipeline behavior for MediatR that validates requests using FluentValidation.
/// Ensures all requests meet business rules before reaching handlers.
/// Implements the Validation pattern with cross-cutting concern injection.
/// 
/// Pattern: Middleware/Interceptor pattern for cross-cutting concerns
/// - Runs before handler execution
/// - Accumulates all validation errors
/// - Throws ValidationException if any errors found
/// - Supports short-circuit failure pattern
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the ValidationBehavior class.
    /// </summary>
    /// <param name="validators">FluentValidation validators for the request type</param>
    /// <param name="logger">Logger instance for validation events</param>
    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    /// <summary>
    /// Validates the request before passing to handler.
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestTypeName = typeof(TRequest).Name;

        if (!_validators.Any())
        {
            _logger.LogDebug("No validators registered for {RequestType}", requestTypeName);
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(validator =>
                validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(result => !result.IsValid)
            .SelectMany(result => result.Errors)
            .ToList();

        if (failures.Any())
        {
            _logger.LogWarning(
                "Validation failed for {RequestType}. Errors: {ValidationErrors}",
                requestTypeName,
                string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}")));

            throw new ValidationException(
                $"Validation failed for {requestTypeName}",
                failures);
        }

        _logger.LogDebug("Validation passed for {RequestType}", requestTypeName);
        return await next();
    }
}
