namespace Reservation.Application.DTOs;

/// <summary>
/// Data Transfer Object for Reservation response.
/// Used to return reservation data to API clients.
/// 
/// DTOs serve as contracts between layers:
/// - Application returns DTOs (not domain entities)
/// - API serializes DTOs to JSON
/// - Hides internal domain structure from clients
/// </summary>
public record ReservationDto(
    /// <summary>Unique identifier for the reservation</summary>
    Guid Id,
    
    /// <summary>Customer who made the reservation</summary>
    Guid CustomerId,
    
    /// <summary>When the reservation starts</summary>
    DateTime StartDate,
    
    /// <summary>When the reservation ends</summary>
    DateTime EndDate,
    
    /// <summary>Current status (Created, Confirmed, Cancelled)</summary>
    string Status,
    
    /// <summary>When the reservation was created</summary>
    DateTime CreatedAt,
    
    /// <summary>When the reservation was last modified</summary>
    DateTime? ModifiedAt
);

/// <summary>
/// Response DTO for reservation creation/confirmation/cancellation operations.
/// Contains the result of the operation and any success/error information.
/// </summary>
public record ReservationOperationResultDto(
    /// <summary>Whether the operation succeeded</summary>
    bool Success,
    
    /// <summary>The reservation ID (present if successful)</summary>
    Guid? ReservationId,
    
    /// <summary>Current status of the reservation</summary>
    string? Status,
    
    /// <summary>Error message if operation failed</summary>
    string? ErrorMessage
);

/// <summary>
/// Maps domain Reservation to ReservationDto.
/// Helper method to convert domain entities to DTOs for API responses.
/// </summary>
public static class ReservationDtoMapping
{
    public static ReservationDto ToDto(Domain.Reservations.Reservation reservation)
    {
        return new ReservationDto(
            reservation.Id,
            reservation.CustomerId,
            reservation.StartDate,
            reservation.EndDate,
            reservation.Status.ToString(),
            reservation.CreatedAt,
            reservation.ModifiedAt
        );
    }

    public static ReservationOperationResultDto ToSuccessResult(Domain.Reservations.Reservation reservation)
    {
        return new ReservationOperationResultDto(
            Success: true,
            ReservationId: reservation.Id,
            Status: reservation.Status.ToString(),
            ErrorMessage: null
        );
    }

    public static ReservationOperationResultDto ToErrorResult(string errorMessage)
    {
        return new ReservationOperationResultDto(
            Success: false,
            ReservationId: null,
            Status: null,
            ErrorMessage: errorMessage
        );
    }
}
