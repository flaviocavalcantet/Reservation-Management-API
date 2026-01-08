using FluentValidation;
using Reservation.API.DTOs;

namespace Reservation.API.Validation;

/// <summary>
/// Validator for GetReservationsRequest.
/// 
/// Enforces API-layer business rules:
/// - CustomerId must be valid and non-empty
/// </summary>
public class GetReservationsValidator : AbstractValidator<GetReservationsRequest>
{
    public GetReservationsValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required and cannot be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("CustomerId must be a valid GUID, not empty GUID");
    }
}
