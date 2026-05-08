# Cache Keys Quick Reference

**Strongly-typed cache key definitions - No hardcoded strings!**

All cache keys are generated using the `ReservationCacheKeys` class. Import and use these static methods throughout your application.

---

## Quick Reference

### Commonly Used

```csharp
using Reservation.Application.Caching;

// Single reservation lookup (GET /api/v1/reservations/{id})
var key = ReservationCacheKeys.ReservationById(reservationId);

// All customer reservations (dashboard "My Reservations")
var key = ReservationCacheKeys.CustomerReservations(customerId);

// Customer reservations filtered by status (pending, confirmed, cancelled)
var key = ReservationCacheKeys.ReservationsByCustomerAndStatus(customerId, "confirmed");

// Availability calendar (which dates are booked?)
var key = ReservationCacheKeys.DateRangeAvailability(startDate, endDate);

// Business rule validation (max 5 active reservations per customer)
var key = ReservationCacheKeys.ActiveReservationCount(customerId);

// Dashboard stats (total, pending, confirmed, etc.)
var key = ReservationCacheKeys.CustomerReservationSummary(customerId);

// Large list pagination
var key = ReservationCacheKeys.PaginatedCustomerReservations(customerId, pageNumber: 1, pageSize: 20);
```

---

## Pattern Matching (Bulk Invalidation)

```csharp
// Invalidate all reservations by ID
var pattern = ReservationCacheKeys.AllReservationByIdPattern();
// → "reservations:byid:*"

// Invalidate all customer lists
var pattern = ReservationCacheKeys.AllCustomerReservationsPattern();
// → "reservations:customer:*"

// Invalidate all status filters for a specific status
var pattern = ReservationCacheKeys.AllReservationsByStatusPattern("confirmed");
// → "reservations:bystatus:confirmed:*"

// Invalidate all availability caches
var pattern = ReservationCacheKeys.AllDateRangeAvailabilityPattern();
// → "reservations:daterange:*"

// Invalidate all active counts
var pattern = ReservationCacheKeys.AllActiveReservationCountPattern();
// → "reservations:count:active:*"

// Invalidate all customer summaries
var pattern = ReservationCacheKeys.AllCustomerReservationSummaryPattern();
// → "reservations:summary:*"

// Invalidate all pages for a specific customer
var pattern = ReservationCacheKeys.AllPaginatedCustomerReservationsPattern(customerId);
// → "reservations:paginated:{customerId}:*"

// ⚠️ NUCLEAR OPTION - Clear everything
var pattern = ReservationCacheKeys.AllReservationCachesPattern();
// → "reservations:*"
```

---

## Implementation Examples

### Handler with Caching

```csharp
public class GetReservationsHandler : IQueryHandler<GetReservationsQuery, IEnumerable<ReservationDto>>
{
    private readonly IReservationRepository _repository;
    private readonly ICacheService _cache;

    public async Task<IEnumerable<ReservationDto>> Handle(
        GetReservationsQuery query,
        CancellationToken cancellationToken)
    {
        // Use GetOrSetAsync for atomic cache-aside pattern
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.CustomerReservations(query.CustomerId),
            async (ct) =>
            {
                var reservations = await _repository.GetByCustomerIdAsync(
                    query.CustomerId,
                    ct);
                
                return reservations
                    .Select(r => ReservationDtoMapping.ToDto(r))
                    .ToList() as IEnumerable<ReservationDto>;
            },
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }
}
```

### Event Handler with Invalidation

```csharp
public class ReservationConfirmedEventHandler 
    : IDomainEventHandler<ReservationConfirmed>
{
    private readonly ICacheInvalidationStrategy _cacheInvalidation;

    public async Task Handle(ReservationConfirmed @event, CancellationToken ct)
    {
        // This invalidates all affected caches
        await _cacheInvalidation.InvalidateReservationConfirmedAsync(
            @event.ReservationId,
            @event.CustomerId,
            ct);
    }
}

// Internally, this invalidates:
// - ReservationById (status changed)
// - CustomerReservations (now confirmed)
// - ReservationsByCustomerAndStatus (moved to confirmed)
// - DateRangeAvailability (dates no longer available)
// - ActiveReservationCount (count updated)
// - CustomerReservationSummary (stats changed)
// - PaginatedCustomerReservations (list changed)
```

### Manual Cache Check

```csharp
// Check if value exists
var exists = await _cache.ExistsAsync(
    ReservationCacheKeys.CustomerReservations(customerId));

if (exists)
    _logger.LogDebug($"Found customer {customerId} in cache");

// Check remaining TTL
var ttl = await _cache.GetTimeToLiveAsync(
    ReservationCacheKeys.CustomerReservations(customerId));

if (ttl > 0)
    _logger.LogDebug($"Cache expires in {ttl} seconds");
else if (ttl == -1)
    _logger.LogDebug("Cache has no expiration");
else if (ttl == -2)
    _logger.LogDebug("Key not found in cache");
```

---

## Anti-Patterns: NEVER DO THIS

### ❌ Hardcoded Cache Keys
```csharp
// WRONG - String typos will break caching
var cached = await _cache.GetAsync<ReservationDto>(
    $"reservations:customer:{customerId}");
```

✅ **Use factory method instead:**
```csharp
var cached = await _cache.GetAsync<ReservationDto>(
    ReservationCacheKeys.CustomerReservations(customerId));
```

---

### ❌ Caching Conflict Detection
```csharp
// WRONG - Risk of overbooking!
var cached = await _cache.GetAsync<bool>(
    ReservationCacheKeys.ReservationConflicts(startDate, endDate));

if (cached == false)
{
    // ❌ DANGEROUS - Might miss real conflicts
    await _repository.ConfirmReservationAsync(...);
}
```

✅ **Always query fresh for conflicts:**
```csharp
// RIGHT - Always check current state
var conflicts = await _repository.GetConflictingReservationsAsync(
    startDate,
    endDate,
    cancellationToken);

if (conflicts.Any())
    throw new ConflictException("Dates are booked");

await _repository.ConfirmReservationAsync(...);
```

---

### ❌ Forgetting to Invalidate
```csharp
// WRONG - Users see stale data
public async Task ConfirmReservation(Guid reservationId, CancellationToken ct)
{
    var reservation = await _repository.GetByIdAsync(reservationId, ct);
    await _repository.ConfirmAsync(reservation, ct);
    
    // ❌ FORGOT TO INVALIDATE CACHE!
}
```

✅ **Use domain event handlers (automatic):**
```csharp
public async Task ConfirmReservation(Guid reservationId, CancellationToken ct)
{
    var reservation = await _repository.GetByIdAsync(reservationId, ct);
    
    // This raises ReservationConfirmed domain event
    reservation.Confirm();
    
    await _repository.SaveAsync(reservation, ct);
    
    // ✅ Domain event handler automatically invalidates cache
    // No need to manually manage cache here
}
```

---

## Cache Key Patterns

| Query | Method | Pattern | Example |
|-------|--------|---------|---------|
| GET single | `ReservationById` | `reservations:byid:{id}` | `reservations:byid:550e8400-e29b` |
| GET list | `CustomerReservations` | `reservations:customer:{id}` | `reservations:customer:550e8400-e29b` |
| GET filtered | `ByCustomerAndStatus` | `reservations:bystatus:{status}:{id}` | `reservations:bystatus:confirmed:550e8400` |
| GET calendar | `DateRangeAvailability` | `reservations:daterange:{start}:{end}` | `reservations:daterange:636000:636001` |
| GET count | `ActiveReservationCount` | `reservations:count:active:{id}` | `reservations:count:active:550e8400` |
| GET summary | `CustomerSummary` | `reservations:summary:{id}` | `reservations:summary:550e8400` |
| GET paged | `Paginated` | `reservations:paginated:{id}:{page}:{size}` | `reservations:paginated:550e:1:20` |

---

## Configuration Example

```csharp
// Program.cs
var builder = WebApplicationBuilder.CreateBuilder(args);

// Configure cache options
builder.Services.Configure<CacheOptions>(options =>
{
    options.Enabled = true;
    options.DefaultTimeToLive = TimeSpan.FromMinutes(5);
    options.MaximumTimeToLive = TimeSpan.FromHours(1);
    options.MinimumTimeToLive = TimeSpan.FromMinutes(1);
});

// Register cache service (Infrastructure layer)
builder.Services.AddStackExchangeRedisCache(options =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"]
        ?? "localhost:6379,abortConnect=false";
    options.Configuration = connectionString;
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ICacheInvalidationStrategy, RedisInvalidationStrategy>();

var app = builder.Build();
```

---

## Testing with Cache Keys

```csharp
[TestFixture]
public class GetReservationsHandlerTests
{
    private IQueryHandler<GetReservationsQuery, IEnumerable<ReservationDto>> _handler;
    private ICacheService _cache;

    [Test]
    public async Task Handle_CachesResult_UsingStronglyTypedKey()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetReservationsQuery(customerId);
        var expectedKey = ReservationCacheKeys.CustomerReservations(customerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Verify cache was used with correct key
        var cached = await _cache.GetAsync<IEnumerable<ReservationDto>>(expectedKey);
        Assert.That(cached, Is.Not.Null);
        Assert.That(cached.Count(), Is.EqualTo(result.Count()));
    }

    [Test]
    public async Task Handle_ClearsCache_OnInvalidation()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var key = ReservationCacheKeys.CustomerReservations(customerId);
        
        // Act
        await _cache.RemoveAsync(key);

        // Assert
        var exists = await _cache.ExistsAsync(key);
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task Handle_SupportsPatternInvalidation()
    {
        // Arrange
        var pattern = ReservationCacheKeys.AllCustomerReservationsPattern();

        // Act
        var removed = await _cache.RemoveByPatternAsync(pattern);

        // Assert
        Assert.That(removed, Is.GreaterThanOrEqualTo(0));
    }
}
```

---

## Finding the Right Cache Key

**"I need to cache a query result - which key should I use?"**

1. **Single reservation lookup?** → `ReservationById`
2. **All of customer's reservations?** → `CustomerReservations`
3. **Filtered by status?** → `ReservationsByCustomerAndStatus`
4. **Calendar/availability?** → `DateRangeAvailability`
5. **Count of active?** → `ActiveReservationCount`
6. **Dashboard stats?** → `CustomerReservationSummary`
7. **Large list with pagination?** → `PaginatedCustomerReservations`

**"I need to add a new query type?"**
→ See [CACHE_KEYS_CATALOG.md](./CACHE_KEYS_CATALOG.md) - "Adding New Cache Keys" section

---

## Documentation References

- **Full Strategy**: [CACHING_STRATEGY.md](../../CACHING_STRATEGY.md)
- **Complete Catalog**: [CACHE_KEYS_CATALOG.md](./CACHE_KEYS_CATALOG.md)
- **Implementation Guide**: [README.md](./README.md)
- **Source Code**: [CacheKeyBuilder.cs](./CacheKeyBuilder.cs)

---

## TL;DR

✅ **Always use `ReservationCacheKeys.MethodName()`** - Never hardcode strings  
✅ **Use `GetOrSetAsync`** for cleaner code  
✅ **Let domain events handle invalidation** - Don't manually manage cache  
✅ **Never cache conflict detection** - Always query fresh  
✅ **Use patterns for bulk operations** - `AllXxxPattern()`  
