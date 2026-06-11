# Caching Strategy - Reservation Management API

Distributed caching using Redis while maintaining Clean Architecture.

---

## Caching Decisions

### ✅ CACHE These Queries

| Query | Key | TTL | Reason |
|-------|-----|-----|--------|
| GetReservationsByCustomer | `reservations:customer:{id}` | 5 min | Read-heavy, changes infrequently |
| GetByDateRange | `reservations:daterange:{start}:{end}` | 10 min | Availability checks, repeating ranges |
| CountActiveByCustomer | `reservations:count:active:{id}` | 5 min | Business rule validation |

See [CACHE_KEYS_CATALOG.md](./src/Reservation.Application/Caching/CACHE_KEYS_CATALOG.md) for complete list.

### ❌ DO NOT CACHE

- **GetConflictingReservations**: Overbooking prevention - must be accurate, always query fresh
- **Individual reservations in edit workflows**: Risk of stale data during updates

---

## Cache Key Naming

Format: `{feature}:{resource}:{identifier}:{variant}`

Example: `reservations:customer:550e8400-e29b-41d4-a716-446655440000`

Rules:
- Lowercase, colon-separated
- Use GUIDs and timestamps, not names
- Include version for schema changes: `reservations:v2:customer:...`

**Use `ReservationCacheKeys` - never hardcode strings!**

See [CACHE_KEYS_QUICK_REFERENCE.md](./src/Reservation.Application/Caching/CACHE_KEYS_QUICK_REFERENCE.md).

---

## TTL (Time-To-Live)

- **Minimum**: 1 minute (prevent cache thrashing)
- **Maximum**: 1 hour (compliance, regulatory)
- **Volatile data** (customer lists, counts): 5 minutes
- **Stable data** (availability): 10 minutes

TTL always explicit. No infinite cache.

---

## Cache Invalidation Strategy

**Event-Driven, not time-based.**

When domain events fire, invalidate affected caches:

| Event | Invalidate |
|-------|-----------|
| ReservationCreated | daterange, count, summary, paginated |
| ReservationConfirmed | customer, status, daterange, count, summary, paginated |
| ReservationCancelled | customer, status, daterange, count, summary, paginated |

Implementation: Domain event handlers call `ICacheInvalidationStrategy` methods.

Pattern-based bulk invalidation:
```csharp
await _cache.RemoveByPatternAsync("reservations:customer:*");  // All customer caches
await _cache.RemoveByPatternAsync("reservations:*");  // Everything (emergency)
```

TTL serves as automatic fallback if invalidation missed.

---

## Clean Architecture Alignment

| Layer | Responsibility |
|-------|-----------------|
| **Domain** | Publishes events (unchanged) |
| **Application** | Defines `ICacheService`, `ICacheInvalidationStrategy` abstractions |
| **Infrastructure** | Implements with Redis, registers event handlers |
| **API** | Zero cache logic in controllers |

No Redis coupling in Application layer.

---

## Configuration

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

## Key Decisions

✅ Event-driven invalidation (maintains consistency)  
✅ Explicit TTL (no infinite cache)  
✅ Pattern-based bulk operations (easier management)  
✅ Strongly-typed cache keys (no hardcoded strings)  
✅ Framework-agnostic abstractions (swap Redis for Memcached)  
✅ Conservative: Don't cache conflict detection (overbooking critical)

---

## References

- [CACHE_KEYS_QUICK_REFERENCE.md](./src/Reservation.Application/Caching/CACHE_KEYS_QUICK_REFERENCE.md) - Developer quick ref
- [CACHE_KEYS_CATALOG.md](./src/Reservation.Application/Caching/CACHE_KEYS_CATALOG.md) - Complete specification
- [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) - Architecture overview
