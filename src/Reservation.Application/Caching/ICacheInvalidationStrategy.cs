namespace Reservation.Application.Caching;

/// <summary>
/// Abstraction for cache invalidation strategy.
/// 
/// Defines how cache entries are invalidated in response to domain events.
/// This keeps invalidation logic organized and testable.
/// 
/// Implementation resides in the Infrastructure layer.
/// 
/// Purpose:
/// - Centralize invalidation rules
/// - Make invalidation policies explicit
/// - Enable different strategies per environment
/// - Support testing with mock strategies
/// 
/// Example:
/// <code>
/// // In domain event handler
/// public class ReservationConfirmedEventHandler
/// {
///     private readonly ICacheInvalidationStrategy _strategy;
///
///     public async Task Handle(ReservationConfirmed @event, CancellationToken ct)
///     {
///         await _strategy.InvalidateReservationConfirmedAsync(@event.ReservationId, @event.CustomerId, ct);
///     }
/// }
/// </code>
/// </summary>
public interface ICacheInvalidationStrategy
{
    /// <summary>
    /// Invalidates cache entries when a new reservation is created.
    ///
    /// Invalidation Cascades:
    /// - Customer's reservation list (list includes all statuses, including newly created)
    /// - All date range availability caches (new reservation affects availability)
    /// - Customer's active reservation count (count increased)
    /// </summary>
    /// <param name="customerId">Customer who created the reservation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateReservationCreatedAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries when a reservation is confirmed.
    /// 
    /// Invalidation Cascades:
    /// - Customer's reservation list (now includes confirmed res)
    /// - All date range availability caches (confirmed res affects availability)
    /// - Customer's active reservation count (status changed to confirmed)
    /// </summary>
    /// <param name="reservationId">ID of confirmed reservation</param>
    /// <param name="customerId">Customer who owns the reservation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateReservationConfirmedAsync(
        Guid reservationId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cache entries when a reservation is cancelled.
    /// 
    /// Invalidation Cascades:
    /// - Customer's reservation list (status changed to cancelled)
    /// - All date range availability caches (cancelled res frees up dates)
    /// - Customer's active reservation count (count decreased)
    /// </summary>
    /// <param name="reservationId">ID of cancelled reservation</param>
    /// <param name="customerId">Customer who owns the reservation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateReservationCancelledAsync(
        Guid reservationId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all reservation-related cache entries.
    /// 
    /// Used for:
    /// - Testing/development
    /// - Manual cache reset commands
    /// - Emergency cache cleanup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ClearAllReservationCachesAsync(
        CancellationToken cancellationToken = default);
}
