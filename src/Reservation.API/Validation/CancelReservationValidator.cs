using FluentValidation;
using Reservation.API.DTOs;

namespace Reservation.API.Validation;

/// <summary>
/// Validator for CancelReservationRequest.
/// 
/// Enforces API-layer business rules:
/// - Reason, if provided, must not be empty
/// 
/// Domain-layer enforces complex business rules (e.g., cannot cancel confirmed reservations after start date).
/// </summary>
public class CancelReservationValidator : AbstractValidator<CancelReservationRequest>
{
    public CancelReservationValidator()
    {
        // Validate Reason - optional but if provided must not be empty/whitespace
        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => !string.IsNullOrEmpty(x.Reason))
            .WithMessage("If provided, Reason cannot be empty or contain only whitespace");
    }
}
