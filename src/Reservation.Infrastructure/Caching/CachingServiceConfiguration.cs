using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reservation.Application.Caching;
using StackExchange.Redis;

namespace Reservation.Infrastructure.Caching;

/// <summary>
/// Extension methods for registering Redis-backed caching services.
///
/// Registers:
/// 1. CacheOptions (bound from the "CacheOptions" configuration section)
/// 2. IConnectionMultiplexer (singleton Redis connection, configured from
///    ConnectionStrings:Redis)
/// 3. ICacheService → RedisCacheService
/// 4. ICacheInvalidationStrategy → RedisCacheInvalidationStrategy
///
/// The Redis connection is configured with AbortOnConnectFail = false so the
/// application can start even if Redis is temporarily unavailable; cache
/// operations degrade to no-ops/cache-misses in that case (see RedisCacheService).
/// </summary>
public static class CachingServiceConfiguration
{
    private const string DefaultRedisConnectionString = "localhost:6379";

    public static IServiceCollection AddCachingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection("CacheOptions").Get<CacheOptions>() ?? new CacheOptions();
        cacheOptions.RedisConnectionString = configuration.GetConnectionString("Redis") ?? DefaultRedisConnectionString;
        cacheOptions.Validate();

        services.AddSingleton(Options.Create(cacheOptions));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var redisConfig = ConfigurationOptions.Parse(cacheOptions.RedisConnectionString);
            redisConfig.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisConfig);
        });

        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<ICacheInvalidationStrategy, RedisCacheInvalidationStrategy>();

        return services;
    }
}
