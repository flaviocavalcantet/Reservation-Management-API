using Reservation.Domain.Abstractions;

namespace Reservation.Domain.Reservations;

/// <summary>
/// Query specifications for Reservation aggregate.
/// Encapsulates common query patterns used throughout the application.
/// 
/// Pattern: Query specifications with business-friendly methods
/// - Improves code readability
/// - Centralizes query logic
/// - Enables query reuse
/// - Easier to test query composition
/// </summary>
/// 
/// <summary>
/// Specification: Get all reservations for a specific customer
/// </summary>
public class ReservationsByCustomerSpecification : Specification<Reservation>
{
    /// <summary>
    /// Initializes a new instance of ReservationsByCustomerSpecification
    /// </summary>
    /// <param name="customerId">The customer ID to filter by</param>
    public ReservationsByCustomerSpecification(Guid customerId)
    {
        Criteria = r => r.CustomerId == customerId;
        ApplyOrderByDescending(r => r.CreatedAt);
    }
}

/// <summary>
/// Specification: Get active reservations (not cancelled, not ended)
/// </summary>
public class ActiveReservationsSpecification : Specification<Reservation>
{
    /// <summary>
    /// Initializes a new instance of ActiveReservationsSpecification
    /// </summary>
    public ActiveReservationsSpecification()
    {
        Criteria = r => r.Status != ReservationStatus.Cancelled
                     && DateTime.UtcNow < r.EndDate;
        ApplyOrderBy(r => r.StartDate);
    }
}

/// <summary>
/// Specification: Get upcoming reservations (not started, not cancelled)
/// </summary>
public class UpcomingReservationsSpecification : Specification<Reservation>
{
    /// <summary>
    /// Initializes a new instance of UpcomingReservationsSpecification
    /// </summary>
    public UpcomingReservationsSpecification()
    {
        Criteria = r => r.Status != ReservationStatus.Cancelled
                     && DateTime.UtcNow < r.StartDate;
        ApplyOrderBy(r => r.StartDate);
    }
}

/// <summary>
/// Specification: Get confirmed reservations for a customer
/// </summary>
public class ConfirmedReservationsForCustomerSpecification : Specification<Reservation>
{
    /// <summary>
    /// Initializes a new instance of ConfirmedReservationsForCustomerSpecification
    /// </summary>
    /// <param name="customerId">The customer ID to filter by</param>
    public ConfirmedReservationsForCustomerSpecification(Guid customerId)
    {
        Criteria = r => r.CustomerId == customerId
                     && r.Status == ReservationStatus.Confirmed;
        ApplyOrderByDescending(r => r.ConfirmedAt ?? r.CreatedAt);
    }
}

/// <summary>
/// Specification: Get reservations by status
/// </summary>
public class ReservationsByStatusSpecification : Specification<Reservation>
{
    /// <summary>
    /// Initializes a new instance of ReservationsByStatusSpecification
    /// </summary>
    /// <param name="status">The reservation status to filter by</param>
    public ReservationsByStatusSpecification(ReservationStatus status)
    {
        Criteria = r => r.Status == status;
        ApplyOrderByDescending(r => r.CreatedAt);
    }
}

/// <summary>
/// Specification: Get paginated reservations for a customer
/// </summary>
public class PaginatedCustomerReservationsSpecification : Specification<Reservation>
{
    /// <summary>
    /// Initializes a new instance of PaginatedCustomerReservationsSpecification
    /// </summary>
    /// <param name="customerId">The customer ID to filter by</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    public PaginatedCustomerReservationsSpecification(Guid customerId, int pageNumber, int pageSize)
    {
        Criteria = r => r.CustomerId == customerId;
        ApplyOrderByDescending(r => r.CreatedAt);
        ApplyPaging((pageNumber - 1) * pageSize, pageSize);
    }
}
