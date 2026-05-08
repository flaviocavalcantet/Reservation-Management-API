namespace Reservation.Application.Caching;

/// <summary>
/// Configuration options for caching behavior.
/// 
/// Allows centralized cache configuration with sane defaults.
/// Injected into services that need cache settings.
/// 
/// Usage:
/// <code>
/// services.Configure&lt;CacheOptions&gt;(options =>
/// {
///     options.DefaultTimeToLive = TimeSpan.FromMinutes(5);
///     options.EnableCaching = true;
/// });
/// </code>
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Whether caching is enabled globally.
    /// 
    /// When disabled, cache operations become no-ops.
    /// Useful for development and testing.
    /// 
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default time-to-live for cache entries when not explicitly specified.
    /// 
    /// Always overrideable per operation.
    /// 
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan DefaultTimeToLive { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum allowed TTL for any cache entry.
    /// Prevents misconfiguration (e.g., caching for days).
    /// 
    /// Default: 1 hour
    /// </summary>
    public TimeSpan MaximumTimeToLive { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Minimum allowed TTL for any cache entry.
    /// Prevents cache thrashing from very short TTLs.
    /// 
    /// Default: 1 minute
    /// </summary>
    public TimeSpan MinimumTimeToLive { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Redis connection string (set by Infrastructure layer).
    /// Not used by Application layer; provided for reference.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Validates cache options for correctness.
    /// 
    /// Throws InvalidOperationException if:
    /// - Minimum > Default
    /// - Default > Maximum
    /// - Minimum >= Maximum
    /// </summary>
    /// <exception cref="InvalidOperationException">If validation fails</exception>
    public void Validate()
    {
        if (MinimumTimeToLive > DefaultTimeToLive)
            throw new InvalidOperationException(
                $"Minimum TTL ({MinimumTimeToLive}) cannot exceed Default TTL ({DefaultTimeToLive})");

        if (DefaultTimeToLive > MaximumTimeToLive)
            throw new InvalidOperationException(
                $"Default TTL ({DefaultTimeToLive}) cannot exceed Maximum TTL ({MaximumTimeToLive})");

        if (MinimumTimeToLive >= MaximumTimeToLive)
            throw new InvalidOperationException(
                $"Minimum TTL ({MinimumTimeToLive}) must be less than Maximum TTL ({MaximumTimeToLive})");
    }

    /// <summary>
    /// Ensures provided TTL is within configured bounds.
    /// Clamps to valid range if out of bounds.
    /// </summary>
    /// <param name="ttl">Desired time-to-live</param>
    /// <returns>Clamped TTL within valid range</returns>
    public TimeSpan ClampTimeToLive(TimeSpan ttl)
    {
        if (ttl < MinimumTimeToLive)
            return MinimumTimeToLive;
        if (ttl > MaximumTimeToLive)
            return MaximumTimeToLive;
        return ttl;
    }
}
