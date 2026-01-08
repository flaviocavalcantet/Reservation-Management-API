namespace Reservation.API.DTOs;

/// <summary>
/// Query parameters DTO for retrieving reservations.
/// 
/// Maps HTTP query parameters to GetReservationsQuery in the application layer.
/// </summary>
public record GetReservationsRequest(
    /// <summary>
    /// Unique identifier of the customer whose reservations to retrieve.
    /// Required - must be a valid non-empty GUID.
    /// </summary>
    Guid CustomerId
);
