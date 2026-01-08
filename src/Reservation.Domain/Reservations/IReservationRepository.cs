using Reservation.Domain.Abstractions;

namespace Reservation.Domain.Reservations;

/// <summary>
/// Repository interface for Reservation aggregate persistence.
/// 
/// Defines the contract for data access operations specific to reservations.
/// The interface lives in the Domain layer; implementation is in Infrastructure.
/// 
/// This keeps the domain completely isolated from persistence concerns while
/// allowing the Application layer to work with domain aggregates.
/// </summary>
public interface IReservationRepository : IRepository<Reservation, Guid>
{
    /// <summary>
    /// Finds all reservations for a specific customer.
    /// Useful for customer-facing features like "My Reservations".
    /// </summary>
    Task<IEnumerable<Reservation>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds reservations within a date range.
    /// Useful for availability checking and calendar views.
    /// </summary>
    Task<IEnumerable<Reservation>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds confirmed reservations that conflict with a given date range.
    /// Critical for preventing double-bookings.
    /// </summary>
    Task<IEnumerable<Reservation>> GetConflictingReservationsAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts active (non-cancelled, non-expired) reservations for a customer.
    /// Useful for analytics and business rules.
    /// </summary>
    Task<int> CountActiveByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}
