namespace Reservation.Application.Caching;

/// <summary>
/// Abstraction for distributed caching operations.
/// 
/// This interface provides a framework-agnostic contract for cache operations.
/// Implementation is provided by the Infrastructure layer (e.g., Redis).
/// 
/// Design Principles:
/// - Framework-agnostic: No Redis/Memcached specific logic
/// - Generic: Works with any serializable type
/// - Async-first: All operations are asynchronous
/// - Cancellation-aware: Supports CancellationToken throughout
/// - Fail-safe: Graceful degradation if cache unavailable
/// 
/// Responsibility:
/// - Store and retrieve typed objects from cache
/// - Manage cache entry expiration
/// - Support efficient bulk operations
/// - Enable cache-aside pattern implementation
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Attempts to retrieve a cached value by key.
    /// Returns null/default if key not found or expired.
    /// 
    /// Usage:
    /// <code>
    /// var cached = await _cacheService.GetAsync&lt;ReservationDto&gt;("reservations:123");
    /// if (cached is null)
    /// {
    ///     var data = await _repository.GetAsync(...);
    ///     await _cacheService.SetAsync("reservations:123", data);
    ///     return data;
    /// }
    /// return cached;
    /// </code>
    /// </summary>
    /// <typeparam name="T">Type of object to retrieve (must be serializable)</typeparam>
    /// <param name="key">Cache key (must not be null or whitespace)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Cached object or null if not found/expired</returns>
    /// <exception cref="ArgumentException">Thrown if key is null, empty, or whitespace</exception>
    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stores a value in cache with explicit time-to-live (TTL).
    /// 
    /// Usage:
    /// <code>
    /// var reservation = await _repository.GetByIdAsync(id);
    /// await _cacheService.SetAsync(
    ///     "reservations:123",
    ///     reservation,
    ///     TimeSpan.FromMinutes(5));
    /// </code>
    /// </summary>
    /// <typeparam name="T">Type of object to cache (must be serializable)</typeparam>
    /// <param name="key">Cache key (must not be null or whitespace)</param>
    /// <param name="value">Object to cache (null values are rejected)</param>
    /// <param name="absoluteExpiration">How long to keep in cache</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Completed task on success</returns>
    /// <exception cref="ArgumentException">Thrown if key is invalid or value is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if TTL is negative or zero</exception>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan absoluteExpiration,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes a cached entry by key.
    /// Safe to call even if key doesn't exist (idempotent).
    /// 
    /// Usage:
    /// <code>
    /// // Remove specific cache entry
    /// await _cacheService.RemoveAsync("reservations:customer:123");
    /// </code>
    /// </summary>
    /// <param name="key">Cache key to remove (must not be null or whitespace)</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Completed task on success</returns>
    /// <exception cref="ArgumentException">Thrown if key is null, empty, or whitespace</exception>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple cache entries using a key pattern.
    /// 
    /// Pattern Syntax:
    /// - '*' matches any sequence of characters
    /// - '?' matches a single character
    /// - Patterns are glob-style (as supported by Redis SCAN MATCH)
    /// 
    /// Usage:
    /// <code>
    /// // Remove all customer reservation caches
    /// await _cacheService.RemoveByPatternAsync("reservations:customer:*");
    /// 
    /// // Remove all date range caches
    /// await _cacheService.RemoveByPatternAsync("reservations:daterange:*");
    /// </code>
    /// </summary>
    /// <param name="pattern">Glob-style pattern for keys to remove</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Number of entries removed</returns>
    /// <exception cref="ArgumentException">Thrown if pattern is null or empty</exception>
    Task<long> RemoveByPatternAsync(
        string pattern,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache-aside pattern: Get or Set atomic operation.
    /// 
    /// If key exists and not expired, returns cached value.
    /// If key doesn't exist, calls valueFactory, caches result, and returns it.
    /// 
    /// Benefits:
    /// - Single operation (no race condition between Get and Set)
    /// - Cleaner code in query handlers
    /// - Better performance (fewer round trips)
    /// 
    /// Usage:
    /// <code>
    /// var reservation = await _cacheService.GetOrSetAsync(
    ///     "reservations:123",
    ///     async (ct) => await _repository.GetByIdAsync(id, ct),
    ///     TimeSpan.FromMinutes(5),
    ///     cancellationToken);
    /// </code>
    /// </summary>
    /// <typeparam name="T">Type of object to cache (must be serializable)</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="valueFactory">Async factory function to call if cache miss</param>
    /// <param name="absoluteExpiration">TTL for cached value</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Cached or freshly fetched value</returns>
    /// <exception cref="ArgumentException">Thrown if key is invalid</exception>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> valueFactory,
        TimeSpan absoluteExpiration,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if a cache key exists without retrieving the value.
    /// Useful for monitoring and debugging.
    /// 
    /// Usage:
    /// <code>
    /// var exists = await _cacheService.ExistsAsync("reservations:123");
    /// </code>
    /// </summary>
    /// <param name="key">Cache key to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if key exists and not expired, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown if key is null or empty</exception>
    Task<bool> ExistsAsync(
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining TTL (time-to-live) of a cached entry in seconds.
    /// 
    /// Return Values:
    /// - Positive number: Seconds until expiration
    /// - -1: Key exists but has no expiration
    /// - -2: Key does not exist
    /// 
    /// Usage:
    /// <code>
    /// var ttl = await _cacheService.GetTimeToLiveAsync("reservations:123");
    /// if (ttl > 0)
    ///     _logger.LogDebug($"Cache expires in {ttl} seconds");
    /// </code>
    /// </summary>
    /// <param name="key">Cache key to check</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Seconds until expiration (-1 if no expiration, -2 if not found)</returns>
    /// <exception cref="ArgumentException">Thrown if key is null or empty</exception>
    Task<long> GetTimeToLiveAsync(
        string key,
        CancellationToken cancellationToken = default);
}
