using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reservation.Application.Caching;
using StackExchange.Redis;

namespace Reservation.Infrastructure.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="ICacheService"/>.
///
/// Uses StackExchange.Redis directly (rather than IDistributedCache) because
/// pattern-based removal (SCAN) and TTL inspection are not exposed by
/// IDistributedCache.
///
/// Fail-safe: any Redis connectivity error is logged and treated as a cache
/// miss/no-op so the application continues to function (reads fall back to
/// the repository) when Redis is unavailable.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly CacheOptions _options;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(
        IConnectionMultiplexer connection,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key must not be null or whitespace", nameof(key));

        if (!_options.Enabled)
            return null;

        try
        {
            var db = _connection.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value!, SerializerOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key {CacheKey}; treating as cache miss", key);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan absoluteExpiration,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key must not be null or whitespace", nameof(key));
        ArgumentNullException.ThrowIfNull(value);
        if (absoluteExpiration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(absoluteExpiration), "TTL must be greater than zero");

        if (!_options.Enabled)
            return;

        try
        {
            var db = _connection.GetDatabase();
            var ttl = _options.ClampTimeToLive(absoluteExpiration);
            var json = JsonSerializer.Serialize(value, SerializerOptions);

            await db.StringSetAsync(key, json, ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {CacheKey}; value was not cached", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key must not be null or whitespace", nameof(key));

        if (!_options.Enabled)
            return;

        try
        {
            var db = _connection.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {CacheKey}", key);
        }
    }

    /// <inheritdoc />
    public async Task<long> RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern must not be null or empty", nameof(pattern));

        if (!_options.Enabled)
            return 0;

        try
        {
            var db = _connection.GetDatabase();
            long removed = 0;

            foreach (var endpoint in _connection.GetEndPoints())
            {
                var server = _connection.GetServer(endpoint);

                await foreach (var key in server.KeysAsync(database: db.Database, pattern: pattern))
                {
                    if (await db.KeyDeleteAsync(key))
                        removed++;
                }
            }

            return removed;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE BY PATTERN failed for pattern {Pattern}", pattern);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> valueFactory,
        TimeSpan absoluteExpiration,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key must not be null or whitespace", nameof(key));

        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var value = await valueFactory(cancellationToken);

        if (value is not null)
            await SetAsync(key, value, absoluteExpiration, cancellationToken);

        return value;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key must not be null or whitespace", nameof(key));

        if (!_options.Enabled)
            return false;

        try
        {
            var db = _connection.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS check failed for key {CacheKey}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> GetTimeToLiveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key must not be null or whitespace", nameof(key));

        if (!_options.Enabled)
            return -2;

        try
        {
            var db = _connection.GetDatabase();
            var ttl = await db.KeyTimeToLiveAsync(key);

            if (ttl is null)
            {
                // Either the key doesn't exist or it exists with no expiration.
                return await db.KeyExistsAsync(key) ? -1 : -2;
            }

            return (long)ttl.Value.TotalSeconds;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache TTL check failed for key {CacheKey}", key);
            return -2;
        }
    }
}
