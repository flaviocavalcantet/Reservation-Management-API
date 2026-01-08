namespace Reservation.API.DTOs;

/// <summary>
/// Request DTO for creating a new reservation.
/// 
/// Maps HTTP request body to CreateReservationCommand in the application layer.
/// Validated before being processed by the application layer.
/// </summary>
public record CreateReservationRequest(
    /// <summary>
    /// Unique identifier of the customer making the reservation.
    /// Must be a valid non-empty GUID.
    /// </summary>
    Guid CustomerId,
    
    /// <summary>
    /// Date and time when the reservation begins.
    /// Must be in the future (at least 1 day from now).
    /// Must be before EndDate.
    /// </summary>
    DateTime StartDate,
    
    /// <summary>
    /// Date and time when the reservation ends.
    /// Must be after StartDate.
    /// Cannot be more than 365 days in the future.
    /// </summary>
    DateTime EndDate
);
