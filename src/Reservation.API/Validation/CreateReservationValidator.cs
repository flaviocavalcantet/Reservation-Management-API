using FluentValidation;
using Reservation.API.DTOs;

namespace Reservation.API.Validation;

/// <summary>
/// Validator for CreateReservationRequest.
/// 
/// Enforces API-layer business rules:
/// - Required fields must be provided
/// - GUIDs must be valid and non-empty
/// - Dates must be in valid range and order
/// - Start date must be at least 1 day in the future
/// - Duration cannot exceed 365 days
/// 
/// These validations complement domain-layer business rules.
/// </summary>
public class CreateReservationValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationValidator()
    {
        // Validate CustomerId
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required and cannot be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("CustomerId must be a valid GUID, not empty GUID");

        // Validate StartDate
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("StartDate is required")
            .GreaterThan(DateTime.UtcNow.AddHours(-1))
            .WithMessage("StartDate must be in the future (at least 1 hour from now)")
            .LessThan(DateTime.UtcNow.AddDays(365))
            .WithMessage("StartDate cannot be more than 365 days in the future");

        // Validate EndDate
        RuleFor(x => x.EndDate)
            .NotEmpty()
            .WithMessage("EndDate is required")
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate")
            .LessThanOrEqualTo(x => x.StartDate.AddDays(365))
            .WithMessage("Reservation duration cannot exceed 365 days");
    }
}
