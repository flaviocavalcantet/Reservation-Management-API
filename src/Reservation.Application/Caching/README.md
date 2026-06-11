# Caching Abstractions - Application Layer

## Overview

Framework-agnostic caching abstractions for the Reservation Management API. Zero Redis coupling.

## Core Interfaces

**ICacheService** - Generic cache operations (get, set, remove, patterns)  
**ICacheInvalidationStrategy** - Event-driven cache invalidation  
**CacheKeyBuilder** - Fluent API for consistent cache keys  
**ReservationCacheKeys** - Strongly-typed factory methods (no hardcoded strings!)  

## Quick Start

### Using Cache in Query Handlers

```csharp
return await _cache.GetOrSetAsync(
    ReservationCacheKeys.CustomerReservations(customerId),
    async (ct) => await _repository.GetByCustomerIdAsync(customerId, ct),
    TimeSpan.FromMinutes(5),
    cancellationToken);
```

### Cache Invalidation (Automatic)

Domain event handlers automatically invalidate affected caches. No manual cache management needed.

```csharp
// When ReservationConfirmed event fires,
// InvalidateReservationConfirmedAsync is called automatically
// Clears: ReservationById, CustomerReservations, DateRangeAvailability, etc.
```

## Key Components

### ICacheService Methods

```csharp
Task<T?> GetAsync<T>(string key, CancellationToken ct)
Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
Task RemoveAsync(string key, CancellationToken ct)
Task<long> RemoveByPatternAsync(string pattern, CancellationToken ct)      // Bulk ops
Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct)  // Recommended
Task<bool> ExistsAsync(string key, CancellationToken ct)
Task<long> GetTimeToLiveAsync(string key, CancellationToken ct)
```

### ICacheInvalidationStrategy Methods

```csharp
Task InvalidateReservationCreatedAsync(Guid customerId, CancellationToken ct)
Task InvalidateReservationConfirmedAsync(Guid reservationId, Guid customerId, CancellationToken ct)
Task InvalidateReservationCancelledAsync(Guid reservationId, Guid customerId, CancellationToken ct)
Task ClearAllReservationCachesAsync(CancellationToken ct)  // Emergency only
```

## Cache Keys

**Use `ReservationCacheKeys` - never hardcode strings!**

Available keys:
- `ReservationById(id)` - Single reservation lookup
- `CustomerReservations(customerId)` - All customer reservations
- `ReservationsByCustomerAndStatus(customerId, status)` - Filtered by status
- `DateRangeAvailability(startDate, endDate)` - Calendar availability
- `ActiveReservationCount(customerId)` - Active reservation count
- `CustomerReservationSummary(customerId)` - Dashboard stats
- `PaginatedCustomerReservations(customerId, page, pageSize)` - Pagination

See [CACHE_KEYS_QUICK_REFERENCE.md](./CACHE_KEYS_QUICK_REFERENCE.md) for complete list and patterns.

## Configuration

```csharp
// Program.cs
builder.Services.Configure<CacheOptions>(options =>
{
    options.Enabled = true;
    options.DefaultTimeToLive = TimeSpan.FromMinutes(5);
    options.MaximumTimeToLive = TimeSpan.FromHours(1);
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();  // Phase 2
builder.Services.AddScoped<ICacheInvalidationStrategy, RedisInvalidationStrategy>();  // Phase 2
```

## Documentation

- **[CACHE_KEYS_QUICK_REFERENCE.md](./CACHE_KEYS_QUICK_REFERENCE.md)** - Developer quick ref (START HERE)
- **[CACHE_KEYS_CATALOG.md](./CACHE_KEYS_CATALOG.md)** - Complete cache key specification
- **[../CACHING_STRATEGY.md](../CACHING_STRATEGY.md)** - Overall caching strategy
- **[../IMPLEMENTATION_SUMMARY.md](../IMPLEMENTATION_SUMMARY.md)** - Architecture overview

## Architecture

Query handlers use `ICacheService` (abstraction).  
Infrastructure layer implements with Redis.  
Domain events trigger `ICacheInvalidationStrategy` automatically.  
No cache logic in controllers.

## Files

- `ICacheService.cs` - Core cache abstraction
- `ICacheInvalidationStrategy.cs` - Event-driven invalidation
- `CacheKeyBuilder.cs` - Fluent cache key generation + `ReservationCacheKeys` factory
- `CacheOptions.cs` - Configuration class
- `CacheMetadata.cs` - Attributes (for future MediatR integration)
