# Cache Keys Catalog

**Centralized, strongly-typed cache key definitions for the Reservation Management API.**

This document serves as the single source of truth for all cache keys used throughout the system. Developers should use the factory methods in `ReservationCacheKeys` class instead of hardcoding cache key strings.

---

## Overview

All cache keys are generated via the `ReservationCacheKeys` static class, which uses `CacheKeyBuilder` for consistent naming and pattern support.

**Naming Convention:**
```
reservations:{resource}:{identifier}:{variant}
```

---

## Cache Key Catalog

### 1. Individual Reservation Lookup

#### **ReservationById**
```csharp
ReservationCacheKeys.ReservationById(reservationId)
// Result: "reservations:byid:550e8400-e29b-41d4-a716-446655440000"
```

**Purpose:** Cache individual reservation details (GET /api/v1/reservations/{id})

**TTL:** 5 minutes

**Invalidation Triggers:**
- `ReservationCreated` (if ID matches)
- `ReservationConfirmed` (status changed)
- `ReservationCancelled` (status changed)

**Use Case:** Prevent repeated database hits for viewing the same reservation

**Example Handler:**
```csharp
public class GetReservationByIdHandler : IQueryHandler<GetReservationByIdQuery, ReservationDto>
{
    public async Task<ReservationDto> Handle(GetReservationByIdQuery query, CancellationToken ct)
    {
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.ReservationById(query.ReservationId),
            async (cancellation) =>
            {
                var reservation = await _repository.GetByIdAsync(query.ReservationId, cancellation);
                return ReservationDtoMapping.ToDto(reservation);
            },
            TimeSpan.FromMinutes(5),
            ct);
    }
}
```

**Pattern for Bulk Operations:**
```csharp
ReservationCacheKeys.AllReservationByIdPattern()
// Result: "reservations:byid:*"
```

---

### 2. Customer Reservation List

#### **CustomerReservations**
```csharp
ReservationCacheKeys.CustomerReservations(customerId)
// Result: "reservations:customer:550e8400-e29b-41d4-a716-446655440000"
```

**Purpose:** Cache customer's complete reservation list (GET /api/v1/reservations?customerId=...)

**TTL:** 5 minutes

**Invalidation Triggers:**
- `ReservationCreated` (for this customer)
- `ReservationConfirmed` (for this customer)
- `ReservationCancelled` (for this customer)

**Use Case:** Dashboard, "My Reservations" view showing all customer bookings

**Example Handler:**
```csharp
public class GetReservationsHandler : IQueryHandler<GetReservationsQuery, IEnumerable<ReservationDto>>
{
    public async Task<IEnumerable<ReservationDto>> Handle(GetReservationsQuery query, CancellationToken ct)
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

**Pattern for Bulk Operations:**
```csharp
ReservationCacheKeys.AllCustomerReservationsPattern()
// Result: "reservations:customer:*"
```

---

### 3. Reservation Status Filter

#### **ReservationsByCustomerAndStatus**
```csharp
ReservationCacheKeys.ReservationsByCustomerAndStatus(customerId, "confirmed")
// Result: "reservations:bystatus:confirmed:550e8400-e29b-41d4-a716-446655440000"
```

**Purpose:** Cache filtered view of customer's reservations by status (e.g., pending, confirmed, cancelled)

**TTL:** 5 minutes

**Invalidation Triggers:**
- `ReservationConfirmed` (when status changes to confirmed)
- `ReservationCancelled` (when status changes to cancelled)

**Use Case:** 
- "Show my pending confirmations"
- "Show my active bookings"
- "Show my cancelled reservations"

**Example Handler:**
```csharp
public class GetReservationsByStatusHandler : IQueryHandler<GetReservationsByStatusQuery, IEnumerable<ReservationDto>>
{
    public async Task<IEnumerable<ReservationDto>> Handle(GetReservationsByStatusQuery query, CancellationToken ct)
    {
        var status = query.Status.Value.ToLowerInvariant();
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.ReservationsByCustomerAndStatus(query.CustomerId, status),
            async (cancellation) =>
            {
                var reservations = await _repository.GetByCustomerIdAsync(query.CustomerId, cancellation);
                var filtered = reservations.Where(r => r.Status.Value.Equals(query.Status.Value, StringComparison.OrdinalIgnoreCase));
                return filtered.Select(r => ReservationDtoMapping.ToDto(r)).ToList() as IEnumerable<ReservationDto>;
            },
            TimeSpan.FromMinutes(5),
            ct);
    }
}
```

**Patterns for Bulk Operations:**
```csharp
// Invalidate all confirmed reservations for a customer
ReservationCacheKeys.AllReservationsByStatusPattern("confirmed")
// Result: "reservations:bystatus:confirmed:*"

// Invalidate all status filters for a customer
ReservationCacheKeys.AllReservationsByStatusPattern()
// Result: "reservations:bystatus:*"
```

---

### 4. Date Range Availability

#### **DateRangeAvailability**
```csharp
ReservationCacheKeys.DateRangeAvailability(new DateTime(2026, 5, 1), new DateTime(2026, 5, 31))
// Result: "reservations:daterange:636000000000000000:636001000000000000"
```

**Purpose:** Cache availability status for date ranges (calendar views, booking widgets)

**TTL:** 10 minutes (longer than other queries; availability is relatively stable)

**Invalidation Triggers:**
- `ReservationCreated` (dates may no longer be available)
- `ReservationCancelled` (dates become available)

**Use Case:** 
- "Which dates are available in May?"
- Calendar widget showing booked/available dates

**Example Handler:**
```csharp
public class GetDateRangeAvailabilityHandler : IQueryHandler<GetDateRangeAvailabilityQuery, AvailabilityDto>
{
    public async Task<AvailabilityDto> Handle(GetDateRangeAvailabilityQuery query, CancellationToken ct)
    {
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.DateRangeAvailability(query.StartDate, query.EndDate),
            async (cancellation) =>
            {
                var reservations = await _repository.GetByDateRangeAsync(
                    query.StartDate,
                    query.EndDate,
                    cancellation);
                return CalculateAvailability(reservations);
            },
            TimeSpan.FromMinutes(10),
            ct);
    }
}
```

**Pattern for Bulk Operations:**
```csharp
ReservationCacheKeys.AllDateRangeAvailabilityPattern()
// Result: "reservations:daterange:*"
```

---

### 5. Active Reservation Count

#### **ActiveReservationCount**
```csharp
ReservationCacheKeys.ActiveReservationCount(customerId)
// Result: "reservations:count:active:550e8400-e29b-41d4-a716-446655440000"
```

**Purpose:** Cache count of active (non-cancelled) reservations for a customer

**TTL:** 5 minutes

**Invalidation Triggers:**
- `ReservationCreated` (count increases)
- `ReservationConfirmed` (status changes, may affect "active" count)
- `ReservationCancelled` (count decreases)

**Use Case:**
- Business rules validation (e.g., "Max 5 active reservations per customer")
- Dashboard statistics
- Analytics and reporting

**Example Handler:**
```csharp
public class GetActiveReservationCountHandler : IQueryHandler<GetActiveReservationCountQuery, int>
{
    public async Task<int> Handle(GetActiveReservationCountQuery query, CancellationToken ct)
    {
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.ActiveReservationCount(query.CustomerId),
            async (cancellation) =>
            {
                return await _repository.CountActiveByCustomerAsync(query.CustomerId, cancellation);
            },
            TimeSpan.FromMinutes(5),
            ct);
    }
}
```

**Pattern for Bulk Operations:**
```csharp
ReservationCacheKeys.AllActiveReservationCountPattern()
// Result: "reservations:count:active:*"
```

---

### 6. Customer Reservation Summary

#### **CustomerReservationSummary**
```csharp
ReservationCacheKeys.CustomerReservationSummary(customerId)
// Result: "reservations:summary:550e8400-e29b-41d4-a716-446655440000"
```

**Purpose:** Cache aggregated statistics about customer's reservations (total, confirmed, pending, etc.)

**TTL:** 5 minutes

**Invalidation Triggers:**
- `ReservationCreated` (stats change)
- `ReservationConfirmed` (stats change)
- `ReservationCancelled` (stats change)

**Use Case:**
- Dashboard widget showing reservation stats
- User profile summary
- Analytics API

**Example Handler:**
```csharp
public class GetCustomerReservationSummaryHandler : IQueryHandler<GetCustomerReservationSummaryQuery, ReservationSummaryDto>
{
    public async Task<ReservationSummaryDto> Handle(GetCustomerReservationSummaryQuery query, CancellationToken ct)
    {
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.CustomerReservationSummary(query.CustomerId),
            async (cancellation) =>
            {
                return await _repository.GetCustomerReservationSummaryAsync(query.CustomerId, cancellation);
            },
            TimeSpan.FromMinutes(5),
            ct);
    }
}
```

**Pattern for Bulk Operations:**
```csharp
ReservationCacheKeys.AllCustomerReservationSummaryPattern()
// Result: "reservations:summary:*"
```

---

### 7. Paginated Customer Reservations

#### **PaginatedCustomerReservations**
```csharp
ReservationCacheKeys.PaginatedCustomerReservations(customerId, pageNumber: 1, pageSize: 20)
// Result: "reservations:paginated:550e8400-e29b-41d4-a716-446655440000:1:20"
```

**Purpose:** Cache individual pages of customer's reservation list with pagination

**TTL:** 5 minutes

**Invalidation Triggers:**
- `ReservationCreated` (invalidate all pages)
- `ReservationConfirmed` (invalidate all pages)
- `ReservationCancelled` (invalidate all pages)

**Use Case:**
- Large reservation lists with page-based navigation
- Reduces database load for users browsing through pages

**Example Handler:**
```csharp
public class GetPaginatedReservationsHandler : IQueryHandler<GetPaginatedReservationsQuery, PagedResult<ReservationDto>>
{
    public async Task<PagedResult<ReservationDto>> Handle(GetPaginatedReservationsQuery query, CancellationToken ct)
    {
        return await _cache.GetOrSetAsync(
            ReservationCacheKeys.PaginatedCustomerReservations(query.CustomerId, query.PageNumber, query.PageSize),
            async (cancellation) =>
            {
                var page = await _repository.GetCustomerReservationsPagedAsync(
                    query.CustomerId,
                    query.PageNumber,
                    query.PageSize,
                    cancellation);
                return new PagedResult<ReservationDto>
                {
                    Items = page.Items.Select(r => ReservationDtoMapping.ToDto(r)).ToList(),
                    TotalCount = page.TotalCount,
                    PageNumber = page.PageNumber,
                    PageSize = page.PageSize
                };
            },
            TimeSpan.FromMinutes(5),
            ct);
    }
}
```

**Pattern for Bulk Operations:**
```csharp
// Invalidate all pages for a customer
ReservationCacheKeys.AllPaginatedCustomerReservationsPattern(customerId)
// Result: "reservations:paginated:550e8400-e29b-41d4-a716-446655440000:*"
```

---

### 8. Comprehensive Invalidation

#### **AllReservationCachesPattern**
```csharp
ReservationCacheKeys.AllReservationCachesPattern()
// Result: "reservations:*"
```

**Purpose:** Emergency or comprehensive cache clearing

**Use Cases:**
- Development/testing: Clear all caches
- Manual admin action: Reset entire reservation cache
- After data migration or recovery

**Example:**
```csharp
// Only in admin endpoints or emergency scenarios
public async Task ClearAllReservationCaches(CancellationToken ct)
{
    await _cacheInvalidation.ClearAllReservationCachesAsync(ct);
    _logger.LogWarning("All reservation caches cleared - manual admin action");
}
```

---

## ⚠️ DO NOT CACHE

### Conflict Detection
```csharp
// ❌ NOT RECOMMENDED - Marked Obsolete
ReservationCacheKeys.ReservationConflicts(startDate, endDate)
```

**Reason:** Overbooking prevention is critical. Conflicts MUST be checked fresh from the database every time.

**Correct Approach:** Always query database directly without caching:
```csharp
// In confirmation flow - ALWAYS fresh
var conflicts = await _repository.GetConflictingReservationsAsync(
    startDate,
    endDate,
    excludeReservationId: null,
    cancellationToken);

if (conflicts.Any())
    throw new ConflictException("Dates already booked");
```

---

## Invalidation Mapping

### When ReservationCreated:
- ❌ `ReservationById` (new, won't exist in cache)
- ❌ `CustomerReservations` (won't be confirmed yet, some filtering might exclude pending)
- ❌ `DateRangeAvailability` (new reservation affects availability) → **INVALIDATE**
- ❌ `ActiveReservationCount` (might not count pending) → **OPTIONAL**
- ❌ `CustomerReservationSummary` → **INVALIDATE**
- ❌ `PaginatedCustomerReservations` → **INVALIDATE ALL PAGES**

### When ReservationConfirmed:
- ✅ `ReservationById` (status changed) → **INVALIDATE**
- ✅ `CustomerReservations` (now visible in list) → **INVALIDATE**
- ✅ `ReservationsByCustomerAndStatus` (moved to confirmed) → **INVALIDATE**
- ✅ `DateRangeAvailability` (confirmed dates no longer available) → **INVALIDATE**
- ✅ `ActiveReservationCount` (confirmed count increases) → **INVALIDATE**
- ✅ `CustomerReservationSummary` (stats changed) → **INVALIDATE**
- ✅ `PaginatedCustomerReservations` → **INVALIDATE ALL PAGES**

### When ReservationCancelled:
- ✅ `ReservationById` (status changed) → **INVALIDATE**
- ✅ `CustomerReservations` (status changed) → **INVALIDATE**
- ✅ `ReservationsByCustomerAndStatus` (moved to cancelled) → **INVALIDATE**
- ✅ `DateRangeAvailability` (cancelled dates now available) → **INVALIDATE**
- ✅ `ActiveReservationCount` (active count decreases) → **INVALIDATE**
- ✅ `CustomerReservationSummary` (stats changed) → **INVALIDATE**
- ✅ `PaginatedCustomerReservations` → **INVALIDATE ALL PAGES**

---

## Best Practices

### 1. Never Hardcode Cache Keys
❌ **WRONG:**
```csharp
var cached = await _cache.GetAsync<ReservationDto>($"reservations:customer:{customerId}");
```

✅ **RIGHT:**
```csharp
var cached = await _cache.GetAsync<ReservationDto>(
    ReservationCacheKeys.CustomerReservations(customerId));
```

### 2. Use GetOrSetAsync for Simplicity
❌ **WRONG:**
```csharp
var cached = await _cache.GetAsync<ReservationDto>(key);
if (cached is null)
{
    var data = await _repository.GetAsync(...);
    await _cache.SetAsync(key, data, TimeSpan.FromMinutes(5));
    return data;
}
return cached;
```

✅ **RIGHT:**
```csharp
return await _cache.GetOrSetAsync(
    key,
    async (ct) => await _repository.GetAsync(..., ct),
    TimeSpan.FromMinutes(5),
    cancellationToken);
```

### 3. Use Factory Methods for Consistency
✅ **CONSISTENT:**
```csharp
var key = ReservationCacheKeys.CustomerReservations(customerId);
var pattern = ReservationCacheKeys.AllCustomerReservationsPattern();
```

### 4. Always Include TTL Strategy
❌ **WRONG:**
```csharp
await _cache.SetAsync(key, data, TimeSpan.FromHours(24));
```

✅ **RIGHT:**
```csharp
// Document in strategy document, justify in code comment
// TTL: 5 minutes (balance between freshness and database load)
await _cache.SetAsync(key, data, TimeSpan.FromMinutes(5), ct);
```

### 5. Test with Specific Keys
✅ **TESTABLE:**
```csharp
[Test]
public async Task GetReservationsByCustomer_CachesResult()
{
    var customerId = Guid.NewGuid();
    var key = ReservationCacheKeys.CustomerReservations(customerId);
    
    // Act
    await _handler.Handle(new GetReservationsQuery(customerId), CancellationToken.None);
    
    // Assert
    var cached = await _cache.GetAsync<IEnumerable<ReservationDto>>(key);
    Assert.That(cached, Is.Not.Null);
}
```

---

## Adding New Cache Keys

When adding a new cacheable query:

1. **Add method to `ReservationCacheKeys`:**
   ```csharp
   public static string NewQueryName(params...) =>
       CacheKeyBuilder
           .ForFeature("reservations")
           .ForResource("resource")
           .WithId(...)
           .Build();
   ```

2. **Add pattern method:**
   ```csharp
   public static string AllNewQueryNamePattern() =>
       CacheKeyBuilder
           .ForFeature("reservations")
           .ForResource("resource")
           .BuildPattern();
   ```

3. **Update CACHE_KEYS_CATALOG.md** with the new key

4. **Update CACHING_STRATEGY.md** invalidation mapping

5. **Implement domain event handler** for invalidation

6. **Add cache integration test**

---

## Debugging Cache Keys

### Redis CLI
```bash
# See all keys
KEYS reservations:*

# See specific pattern
KEYS reservations:customer:*

# Get key details
TTL reservations:customer:550e8400-e29b-41d4-a716-446655440000

# Get value
GET reservations:customer:550e8400-e29b-41d4-a716-446655440000
```

### .NET Code
```csharp
// Check if key exists
var exists = await _cache.ExistsAsync(ReservationCacheKeys.CustomerReservations(customerId));

// Check TTL
var ttl = await _cache.GetTimeToLiveAsync(ReservationCacheKeys.CustomerReservations(customerId));
_logger.LogDebug($"Cache expires in {ttl} seconds");

// Manual invalidation for testing
await _cache.RemoveAsync(ReservationCacheKeys.CustomerReservations(customerId));
```

---

## Summary Table

| Cache Key | Pattern | TTL | Invalidation Triggers | Use Case |
|-----------|---------|-----|----------------------|----------|
| `ReservationById` | `reservations:byid:*` | 5 min | Create, Confirm, Cancel | GET single reservation |
| `CustomerReservations` | `reservations:customer:*` | 5 min | Create, Confirm, Cancel | GET all customer reservations |
| `ByCustomerAndStatus` | `reservations:bystatus:*` | 5 min | Confirm, Cancel | Filter by status |
| `DateRangeAvailability` | `reservations:daterange:*` | 10 min | Create, Cancel | Calendar/availability view |
| `ActiveReservationCount` | `reservations:count:active:*` | 5 min | Create, Confirm, Cancel | Quota validation |
| `CustomerSummary` | `reservations:summary:*` | 5 min | Create, Confirm, Cancel | Dashboard stats |
| `Paginated` | `reservations:paginated:*` | 5 min | Create, Confirm, Cancel | Large list pagination |

---

## References

- [CacheKeyBuilder.cs](../CacheKeyBuilder.cs) - Implementation
- [CACHING_STRATEGY.md](../../CACHING_STRATEGY.md) - Strategy documentation
- [README.md](./README.md) - Usage guide
