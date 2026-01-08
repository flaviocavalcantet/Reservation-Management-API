namespace Reservation.API.DTOs;

/// <summary>
/// Request DTO for cancelling an existing reservation.
/// 
/// Maps HTTP request body to CancelReservationCommand in the application layer.
/// </summary>
public record CancelReservationRequest(
    /// <summary>
    /// Reason for cancellation - recorded in audit trail.
    /// Can be null, but if provided must be non-empty.
    /// Examples: "Guest requested cancellation", "Double booking error", etc.
    /// </summary>
    string? Reason = null
);
