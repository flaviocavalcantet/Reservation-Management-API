# Caching Implementation - Architecture Summary

**Date**: May 8, 2026  
**Status**: ✅ Phase 1 Complete - Abstractions Defined  
**Next**: Phase 2 - Redis Implementation

---

## Executive Summary

We have successfully defined a **production-grade, Clean Architecture-compliant caching strategy** for the Reservation Management API using distributed caching with Redis. The implementation maintains clean architecture principles by:

- ✅ **Abstraction-First Design**: Core caching logic resides in Application layer as interfaces
- ✅ **No Redis Coupling**: Application layer has zero Redis dependencies
- ✅ **Event-Driven Invalidation**: Domain events trigger explicit cache management
- ✅ **Framework-Agnostic**: Can swap Redis for Memcached, Hazelcast, etc.
- ✅ **Production-Ready**: Includes error handling, TTL strategies, and observability hooks

---

## Phase 1: Abstractions (COMPLETE ✅)

### 1. Core Interfaces Defined

#### ICacheService
Location: `src/Reservation.Application/Caching/ICacheService.cs`

Generic abstraction for distributed caching operations:

```csharp
Task<T?> GetAsync<T>(string key, CancellationToken ct)
Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
Task RemoveAsync(string key, CancellationToken ct)
Task<long> RemoveByPatternAsync(string pattern, CancellationToken ct)
Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct)
Task<bool> ExistsAsync(string key, CancellationToken ct)
Task<long> GetTimeToLiveAsync(string key, CancellationToken ct)
```

**Key Design Decisions:**
- Fully async/await pattern
- CancellationToken support throughout
- Generic with `class` constraint (for serialization)
- Explicit error handling with ArgumentException
- TTL always explicit (no infinite cache)
- Batch pattern removal for cascade invalidation

---

#### ICacheInvalidationStrategy
Location: `src/Reservation.Application/Caching/ICacheInvalidationStrategy.cs`

Event-driven cache invalidation interface:

```csharp
Task InvalidateReservationCreatedAsync(Guid customerId, CancellationToken ct)
Task InvalidateReservationConfirmedAsync(Guid reservationId, Guid customerId, CancellationToken ct)
Task InvalidateReservationCancelledAsync(Guid reservationId, Guid customerId, CancellationToken ct)
Task ClearAllReservationCachesAsync(CancellationToken ct)
```

**Key Design Decisions:**
- Separate from ICacheService for testability
- Domain-specific methods (not generic)
- Always receives required context (customerId, reservationId)
- Supports both targeted and bulk invalidation

---

### 2. Utility Classes

#### CacheKeyBuilder
Location: `src/Reservation.Application/Caching/CacheKeyBuilder.cs`

Fluent API for consistent cache key generation.

**Features:**
- Format: `{feature}:{version}:{resource}:{identifier}:{variant}`
- Fluent builder pattern for clarity
- Version support for non-breaking cache updates
- Pattern building for bulk operations

**Example:**
```csharp
var key = CacheKeyBuilder
    .ForFeature("reservations")
    .ForResource("customer")
    .WithId(customerId)
    .Build();
// Result: "reservations:customer:550e8400-e29b-41d4-a716-446655440000"
```

---

#### ReservationCacheKeys
Location: `src/Reservation.Application/Caching/CacheKeyBuilder.cs` (included)

Static factory methods for domain-specific keys:

```csharp
ReservationCacheKeys.CustomerReservations(customerId)
ReservationCacheKeys.DateRangeAvailability(startDate, endDate)
ReservationCacheKeys.ActiveReservationCount(customerId)

// Patterns:
ReservationCacheKeys.AllCustomerReservationsPattern()
ReservationCacheKeys.AllDateRangeAvailabilityPattern()
ReservationCacheKeys.AllActiveReservationCountPattern()
```

---

#### CacheOptions
Location: `src/Reservation.Application/Caching/CacheOptions.cs`

Configuration class with validation:

```csharp
public class CacheOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan DefaultTimeToLive { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan MaximumTimeToLive { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan MinimumTimeToLive { get; set; } = TimeSpan.FromMinutes(1);
    public string? RedisConnectionString { get; set; }
    
    public void Validate() { ... }
    public TimeSpan ClampTimeToLive(TimeSpan ttl) { ... }
}
```

---

#### CacheMetadata
Location: `src/Reservation.Application/Caching/CacheMetadata.cs`

Attributes and helper classes for future MediatR integration:

```csharp
[Cacheable(DurationSeconds = 300, KeyPattern = "reservations:customer:{CustomerId}")]
public record GetReservationsQuery(Guid CustomerId) : IQuery<IEnumerable<ReservationDto>>;

[NoCacheable("Prevents overbooking - must check current state")]
public record GetConflictingReservationsQuery(...) : IQuery<...>;

public class CacheResult
{
    public bool WasHit { get; set; }
    public string? CacheKey { get; set; }
    public string? SkipReason { get; set; }
    public long DurationMs { get; set; }
}
```

---

## Strategy Definition (Complete ✅)

### Caching Decisions Matrix

| Query | Cache? | TTL | Invalidation Triggers |
|-------|--------|-----|----------------------|
| `GetReservationsByCustomer` | ✅ | 5 min | Create, Confirm, Cancel |
| `GetByDateRange` | ✅ | 10 min | Create, Cancel |
| `CountActiveByCustomer` | ✅ | 5 min | Create, Confirm, Cancel |
| `GetConflictingReservations` | ❌ | N/A | ALWAYS fresh (overbooking) |

### Cache Key Naming Convention

```
Pattern: {feature}:{version}:{resource}:{identifier}:{variant}

Examples:
- reservations:customer:550e8400-e29b-41d4-a716-446655440000
- reservations:v2:customer:550e8400-e29b-41d4-a716-446655440000
- reservations:daterange:636000000000000000:636001000000000000
- reservations:count:active:550e8400-e29b-41d4-a716-446655440000
```

### Invalidation Strategy (Event-Driven)

```
Domain Event                 → Invalidated Cache Keys
───────────────────────────   ─────────────────────────────────
ReservationCreated          → reservations:daterange:*
                            → reservations:count:active:{customerId}

ReservationConfirmed        → reservations:customer:{customerId}
                            → reservations:count:active:{customerId}
                            → reservations:daterange:*

ReservationCancelled        → reservations:customer:{customerId}
                            → reservations:count:active:{customerId}
                            → reservations:daterange:*
```

---

## Clean Architecture Alignment

### Application Layer (This Phase ✅)
- **ICacheService**: Core abstraction
- **ICacheInvalidationStrategy**: Event-driven invalidation
- **Cache Key Utilities**: Consistent key management
- **Configuration**: CacheOptions
- **Metadata**: Attributes for future integration

**No Redis dependencies in this layer** ✅

### Infrastructure Layer (Phase 2 - TODO)
- **RedisCacheService**: Implements ICacheService
- **RedisInvalidationStrategy**: Implements ICacheInvalidationStrategy
- **Redis Connection Factory**: Connection pooling
- **Domain Event Handlers**: Wire invalidation to events
- **Health Checks**: Redis availability monitoring

### API Layer (Phase 3 - TODO)
- No cache logic in controllers/endpoints
- Caching transparent via MediatR pipeline
- Query handlers use cache abstraction

### Domain Layer
- Unchanged (publishes events)
- No cache awareness

---

## Usage Examples (Ready for Implementation)

### Pattern 1: Cache-Aside (Recommended)

```csharp
public class GetReservationsHandler : IQueryHandler<GetReservationsQuery, IEnumerable<ReservationDto>>
{
    private readonly IReservationRepository _repository;
    private readonly ICacheService _cache;

    public async Task<IEnumerable<ReservationDto>> Handle(
        GetReservationsQuery query,
        CancellationToken ct)
    {
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.CustomerReservations(query.CustomerId),
            async (cancellation) =>
            {
                var reservations = await _repository.GetByCustomerIdAsync(query.CustomerId, cancellation);
                return reservations.Select(r => ReservationDtoMapping.ToDto(r)).ToList() as IEnumerable<ReservationDto>;
            },
            TimeSpan.FromMinutes(5),
            ct);
    }
}
```

### Pattern 2: Event-Driven Invalidation

```csharp
public class ReservationConfirmedEventHandler : IDomainEventHandler<ReservationConfirmed>
{
    private readonly ICacheInvalidationStrategy _cacheInvalidation;

    public async Task Handle(ReservationConfirmed @event, CancellationToken ct)
    {
        await _cacheInvalidation.InvalidateReservationConfirmedAsync(
            @event.ReservationId,
            @event.CustomerId,
            ct);
    }
}
```

### Pattern 3: No-Cache Marker

```csharp
[NoCacheable("Prevents overbooking - must always check current state")]
public record GetConflictingReservationsQuery(
    DateTime StartDate,
    DateTime EndDate,
    Guid? ExcludeReservationId = null
) : IQuery<IEnumerable<Reservation>>;
```

---

## File Structure

```
src/Reservation.Application/
└── Caching/
    ├── ICacheService.cs              ✅ Core abstraction
    ├── ICacheInvalidationStrategy.cs ✅ Event-driven invalidation
    ├── CacheOptions.cs               ✅ Configuration
    ├── CacheKeyBuilder.cs            ✅ Key generation utilities
    ├── CacheMetadata.cs              ✅ Attributes for future use
    └── README.md                     ✅ Comprehensive documentation
```

---

## Next Steps (Phase 2 - Infrastructure Implementation)

### 2.1 Create Redis Implementation
```
src/Reservation.Infrastructure/
└── Caching/
    ├── RedisCacheService.cs          (implements ICacheService)
    ├── RedisInvalidationStrategy.cs  (implements ICacheInvalidationStrategy)
    ├── RedisConnectionFactory.cs     (connection pooling)
    └── HealthChecks/
        └── RedisCacheHealthCheck.cs
```

### 2.2 Register Dependency Injection
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = cacheOptions.RedisConnectionString;
});

services.AddScoped<ICacheService, RedisCacheService>();
services.AddScoped<ICacheInvalidationStrategy, RedisInvalidationStrategy>();
services.Configure<CacheOptions>(configuration.GetSection("Caching"));
```

### 2.3 Wire Domain Events
Register event handlers to trigger cache invalidation:
```csharp
services.RegisterDomainEventHandlers();
```

### 2.4 Update Query Handlers
Integrate caching into:
- `GetReservationsHandler`
- `GetByDateRangeHandler` (if exists)
- `CountActiveByCustomerHandler` (if exists)

### 2.5 Add Tests
- Unit tests for cache key builders
- Integration tests with TestContainers Redis
- Metrics and observability

---

## Production Considerations

### Security
- ✅ Plan: Use separate Redis DB per environment
- ✅ Plan: Enable Redis AUTH
- ✅ Plan: Use TLS for connections
- ✅ Plan: Store connection strings in Azure Key Vault

### Performance
- ✅ Plan: Connection pooling via StackExchange.Redis
- ✅ Plan: Binary serialization (MessagePack)
- ✅ Plan: GZip compression for large objects
- ✅ Plan: Monitor hit rate and eviction rate

### Reliability
- ✅ Plan: Graceful fallback to database if Redis unavailable
- ✅ Plan: Exponential backoff on cache failures
- ✅ Plan: Circuit breaker for cascading failures
- ✅ Plan: Comprehensive logging/observability

### Scalability
- ✅ Plan: Redis persistence (RDB snapshots)
- ✅ Plan: Redis cluster for high availability
- ✅ Plan: Master-slave replication
- ✅ Plan: Sharding if needed

---

## Configuration Example (appsettings.json)

```json
{
  "Caching": {
    "Enabled": true,
    "DefaultTimeToLive": 300,
    "MaximumTimeToLive": 3600,
    "MinimumTimeToLive": 60,
    "RedisConnectionString": "localhost:6379,abortConnect=false"
  }
}
```

---

## Monitoring & Observability (Phase 5)

### Metrics to Collect
- Cache hit rate
- Cache miss rate
- Eviction rate
- Memory usage
- Key count by pattern
- Invalidation event count

### Logging
- Cache key access patterns
- Invalidation events
- Redis connection issues
- Serialization errors

---

## Documentation

### Created Files
1. **CACHING_STRATEGY.md**: Complete strategy document with all decisions
2. **src/Reservation.Application/Caching/README.md**: Usage guide and implementation checklist
3. **IMPLEMENTATION_SUMMARY.md**: This file (architecture overview)

### Key Decisions Documented
- Why certain queries are cached
- Why conflict detection is never cached
- TTL strategy justification
- Event-driven invalidation rationale
- Clean Architecture adherence

---

## Success Criteria (Phase 1 ✅)

- [x] Abstraction-first design with no Redis coupling
- [x] Framework-agnostic interfaces
- [x] Comprehensive cache strategy document
- [x] Key naming convention defined and implemented
- [x] Invalidation strategy abstraction
- [x] TTL management utilities
- [x] Production-grade error handling
- [x] CancellationToken support throughout
- [x] Clear usage patterns documented
- [x] Type-safe cache key builders
- [x] Domain-specific key factory methods
- [x] Configuration validation
- [x] Ready for Infrastructure implementation

---

## Architectural Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                         API Layer                              │
│  (ReservationEndpoints - NO cache logic here)                  │
└────────────────────┬───────────────────────────────────────────┘
                     │ MediatR Query/Command
                     ▼
┌────────────────────────────────────────────────────────────────┐
│                    Application Layer                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Query Handlers (with caching)                            │  │
│  │ - GetReservationsHandler                                 │  │
│  │ - Uses ICacheService.GetOrSetAsync()                     │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Abstractions (THIS PHASE)                                │  │
│  │ ✅ ICacheService                                          │  │
│  │ ✅ ICacheInvalidationStrategy                             │  │
│  │ ✅ CacheOptions, CacheKeyBuilder, etc.                    │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ Domain Events (unchanged)                                │  │
│  │ - ReservationCreated, Confirmed, Cancelled               │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────┬───────────────────────────────────────────┘
                     │ Implement in Phase 2
                     ▼
┌────────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                           │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ RedisCacheService (PHASE 2)                              │  │
│  │ - Implements ICacheService                               │  │
│  │ - Uses StackExchange.Redis client                         │  │
│  │ - Connection pooling, serialization                       │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ RedisInvalidationStrategy (PHASE 2)                      │  │
│  │ - Implements ICacheInvalidationStrategy                   │  │
│  │ - Wired to domain events                                  │  │
│  │ - Handles cascade invalidation                            │  │
│  └──────────────────────────────────────────────────────────┘  │
│                           │                                     │
│                           ▼                                     │
│                    ┌──────────────┐                            │
│                    │  Redis Cache │                            │
│                    │   Cluster    │                            │
│                    └──────────────┘                            │
└────────────────────────────────────────────────────────────────┘
```

---

## Conclusion

**Phase 1 is complete.** The Reservation Management API now has a solid, Clean Architecture-compliant caching foundation with:

✅ Framework-agnostic abstractions  
✅ Production-grade interfaces  
✅ Comprehensive strategy documentation  
✅ Type-safe utilities  
✅ Event-driven invalidation pattern  
✅ Ready for Redis implementation  

The abstractions are ready for the Infrastructure layer to implement Redis-specific functionality while maintaining complete separation of concerns.

---

**Next: Phase 2 - Infrastructure Redis Implementation** 🚀
