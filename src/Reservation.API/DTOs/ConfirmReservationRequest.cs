namespace Reservation.API.DTOs;

/// <summary>
/// Request DTO for confirming an existing reservation.
/// 
/// Maps HTTP request to ConfirmReservationCommand in the application layer.
/// The reservation ID is provided in the route parameter.
/// </summary>
public record ConfirmReservationRequest
{
    /// <summary>
    /// No request body required - all information comes from the route parameter.
    /// This record exists for API consistency and future extensibility.
    /// </summary>
}
