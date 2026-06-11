using Microsoft.Extensions.Logging;
using Reservation.Application.Caching;

namespace Reservation.Infrastructure.Caching;

/// <summary>
/// Default cache invalidation strategy for reservation-related cache entries.
///
/// Implements the cascades documented on <see cref="ICacheInvalidationStrategy"/>
/// using <see cref="ICacheService"/> key removal (single-key and pattern-based).
/// </summary>
public class RedisCacheInvalidationStrategy : ICacheInvalidationStrategy
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<RedisCacheInvalidationStrategy> _logger;

    public RedisCacheInvalidationStrategy(
        ICacheService cacheService,
        ILogger<RedisCacheInvalidationStrategy> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InvalidateReservationCreatedAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Invalidating caches after reservation created for customer {CustomerId}", customerId);

        // The customer's reservation list cache includes all statuses (see
        // GetReservationsHandler), so a newly created reservation must
        // invalidate it to avoid serving a stale list.
        await _cacheService.RemoveAsync(ReservationCacheKeys.CustomerReservations(customerId), cancellationToken);
        await _cacheService.RemoveAsync(ReservationCacheKeys.ActiveReservationCount(customerId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(ReservationCacheKeys.AllDateRangeAvailabilityPattern(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvalidateReservationConfirmedAsync(
        Guid reservationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Invalidating caches after reservation {ReservationId} confirmed for customer {CustomerId}",
            reservationId,
            customerId);

        await _cacheService.RemoveAsync(ReservationCacheKeys.CustomerReservations(customerId), cancellationToken);
        await _cacheService.RemoveAsync(ReservationCacheKeys.ReservationById(reservationId), cancellationToken);
        await _cacheService.RemoveAsync(ReservationCacheKeys.ActiveReservationCount(customerId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(ReservationCacheKeys.AllDateRangeAvailabilityPattern(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task InvalidateReservationCancelledAsync(
        Guid reservationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Invalidating caches after reservation {ReservationId} cancelled for customer {CustomerId}",
            reservationId,
            customerId);

        await _cacheService.RemoveAsync(ReservationCacheKeys.CustomerReservations(customerId), cancellationToken);
        await _cacheService.RemoveAsync(ReservationCacheKeys.ReservationById(reservationId), cancellationToken);
        await _cacheService.RemoveAsync(ReservationCacheKeys.ActiveReservationCount(customerId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(ReservationCacheKeys.AllDateRangeAvailabilityPattern(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task ClearAllReservationCachesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Clearing all reservation cache entries");

        await _cacheService.RemoveByPatternAsync(ReservationCacheKeys.AllReservationCachesPattern(), cancellationToken);
    }
}
