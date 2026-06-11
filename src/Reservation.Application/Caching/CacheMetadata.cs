namespace Reservation.Application.Caching;

/// <summary>
/// Metadata for controlling cache behavior at the query level.
/// 
/// Enables declarative cache configuration on query handlers.
/// Used by MediatR pipeline behaviors for automatic caching.
/// 
/// Usage:
/// <code>
/// [Cacheable(
///     Duration = 300, // 5 minutes
///     KeyPattern = "reservations:customer:{customerId}")]
/// public record GetReservationsQuery(Guid CustomerId) : IQuery&lt;IEnumerable&lt;ReservationDto&gt;&gt;;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CacheableAttribute : Attribute
{
    /// <summary>
    /// Cache duration in seconds.
    /// </summary>
    public int DurationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Cache key pattern (with placeholders for property names).
    /// Placeholders use {PropertyName} format.
    /// 
    /// Example:
    /// "reservations:customer:{CustomerId}" for GetReservationsQuery
    /// "reservations:daterange:{StartDate}:{EndDate}" for GetAvailabilityQuery
    /// </summary>
    public string KeyPattern { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable caching for this query.
    /// Allows temporarily disabling cache without removing attribute.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Indicates that a query should NOT be cached.
/// Explicit marker for queries that must always hit the database.
/// 
/// Usage:
/// <code>
/// [NoCacheable("Prevents overbooking - must check current state")]
/// public record GetConflictingReservationsQuery(...) : IQuery&lt;IEnumerable&lt;Reservation&gt;&gt;;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class NoCacheableAttribute : Attribute
{
    /// <summary>
    /// Reason why this query should never be cached.
    /// Helps developers understand the decision.
    /// </summary>
    public string Reason { get; set; }

    public NoCacheableAttribute(string reason)
    {
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }
}

/// <summary>
/// Result of attempting to cache a query response.
/// 
/// Tracks whether caching was used or skipped and why.
/// Useful for diagnostics and metrics collection.
/// </summary>
public class CacheResult
{
    /// <summary>
    /// Whether the response was served from cache.
    /// </summary>
    public bool WasHit { get; set; }

    /// <summary>
    /// Cache key that was used (null if no caching attempted).
    /// </summary>
    public string? CacheKey { get; set; }

    /// <summary>
    /// Reason caching was skipped (null if caching was used).
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Duration in milliseconds to retrieve from cache or database.
    /// </summary>
    public long DurationMs { get; set; }

    public static CacheResult Hit(string cacheKey, long durationMs) =>
        new()
        {
            WasHit = true,
            CacheKey = cacheKey,
            DurationMs = durationMs
        };

    public static CacheResult Miss(string cacheKey, long durationMs) =>
        new()
        {
            WasHit = false,
            CacheKey = cacheKey,
            SkipReason = "Cache miss",
            DurationMs = durationMs
        };

    public static CacheResult Skipped(string reason, long durationMs) =>
        new()
        {
            WasHit = false,
            SkipReason = reason,
            DurationMs = durationMs
        };
}
