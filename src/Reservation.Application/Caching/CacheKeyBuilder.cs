namespace Reservation.Application.Caching;

/// <summary>
/// Utility class for generating consistent cache keys.
/// 
/// Ensures all cache keys follow the naming convention:
/// {feature}:{resource}:{identifier}:{variant}
/// 
/// Benefits:
/// - Centralized key management
/// - Easy to find related cache keys
/// - Enables pattern-based bulk invalidation
/// - Supports versioning for non-breaking updates
/// 
/// Usage:
/// <code>
/// var key = CacheKeyBuilder
///     .ForFeature("reservations")
///     .ForResource("customer")
///     .WithId(customerId)
///     .Build();
/// // Result: "reservations:customer:550e8400-e29b-41d4-a716-446655440000"
/// </code>
/// </summary>
public class CacheKeyBuilder
{
    private const string Separator = ":";
    private const char Version = '2';
    
    private string? _feature;
    private string? _resource;
    private string? _identifier;
    private string? _variant;
    private bool _includeVersion;

    /// <summary>
    /// Initializes a cache key builder for a feature.
    /// </summary>
    /// <param name="feature">Feature name (e.g., "reservations", "customers")</param>
    /// <returns>Builder for chaining</returns>
    public static CacheKeyBuilder ForFeature(string feature)
    {
        if (string.IsNullOrWhiteSpace(feature))
            throw new ArgumentException("Feature must not be empty", nameof(feature));

        var builder = new CacheKeyBuilder();
        builder._feature = Normalize(feature);
        return builder;
    }

    /// <summary>
    /// Specifies the resource type within the feature.
    /// </summary>
    /// <param name="resource">Resource type (e.g., "customer", "daterange", "count")</param>
    /// <returns>Builder for chaining</returns>
    public CacheKeyBuilder ForResource(string resource)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource must not be empty", nameof(resource));

        _resource = Normalize(resource);
        return this;
    }

    /// <summary>
    /// Adds an identifier to the key (usually an entity ID or key parameter).
    /// </summary>
    /// <param name="id">Identifier value</param>
    /// <returns>Builder for chaining</returns>
    public CacheKeyBuilder WithId(Guid id)
    {
        _identifier = id.ToString();
        return this;
    }

    /// <summary>
    /// Adds an identifier to the key for non-GUID identifiers.
    /// </summary>
    /// <param name="id">Identifier value (converted to string)</param>
    /// <returns>Builder for chaining</returns>
    public CacheKeyBuilder WithId(object id)
    {
        if (id is null)
            throw new ArgumentNullException(nameof(id));

        _identifier = Normalize(id.ToString() ?? throw new InvalidOperationException(
            $"Cannot convert {id.GetType().Name} to cache key identifier"));
        return this;
    }

    /// <summary>
    /// Adds a range component to the key (for date range queries).
    /// </summary>
    /// <param name="startDate">Range start</param>
    /// <param name="endDate">Range end</param>
    /// <returns>Builder for chaining</returns>
    public CacheKeyBuilder WithDateRange(DateTime startDate, DateTime endDate)
    {
        _identifier = $"{startDate.Ticks}:{endDate.Ticks}";
        return this;
    }

    /// <summary>
    /// Adds a variant suffix for cache versioning or differentiation.
    /// Useful when you need multiple cache entries for similar queries.
    /// </summary>
    /// <param name="variant">Variant name (e.g., "active", "pending", "v2")</param>
    /// <returns>Builder for chaining</returns>
    public CacheKeyBuilder WithVariant(string variant)
    {
        if (string.IsNullOrWhiteSpace(variant))
            throw new ArgumentException("Variant must not be empty", nameof(variant));

        _variant = Normalize(variant);
        return this;
    }

    /// <summary>
    /// Includes version number in the key for non-breaking cache updates.
    /// When schema changes, bump version to invalidate old cache entries.
    /// </summary>
    /// <returns>Builder for chaining</returns>
    public CacheKeyBuilder IncludeVersion()
    {
        _includeVersion = true;
        return this;
    }

    /// <summary>
    /// Builds the final cache key string.
    /// </summary>
    /// <returns>Formatted cache key</returns>
    /// <exception cref="InvalidOperationException">If required components are missing</exception>
    public string Build()
    {
        if (string.IsNullOrEmpty(_feature))
            throw new InvalidOperationException("Feature is required");
        if (string.IsNullOrEmpty(_resource))
            throw new InvalidOperationException("Resource is required");
        if (string.IsNullOrEmpty(_identifier))
            throw new InvalidOperationException("Identifier is required");

        var parts = new List<string> { _feature };

        if (_includeVersion)
            parts.Add($"v{Version}");

        parts.Add(_resource);
        parts.Add(_identifier);

        if (!string.IsNullOrEmpty(_variant))
            parts.Add(_variant);

        return string.Join(Separator, parts);
    }

    /// <summary>
    /// Builds a pattern for wildcard cache key matching.
    /// 
    /// Usage:
    /// <code>
    /// var pattern = CacheKeyBuilder
    ///     .ForFeature("reservations")
    ///     .ForResource("customer")
    ///     .BuildPattern();
    /// // Result: "reservations:customer:*"
    /// </code>
    /// </summary>
    /// <returns>Pattern string with wildcards for SCAN operations</returns>
    public string BuildPattern()
    {
        if (string.IsNullOrEmpty(_feature))
            throw new InvalidOperationException("Feature is required");
        if (string.IsNullOrEmpty(_resource))
            throw new InvalidOperationException("Resource is required");

        var parts = new List<string> { _feature };

        if (_includeVersion)
            parts.Add($"v{Version}");

        parts.Add(_resource);

        // Add wildcard at the end for pattern matching
        if (!string.IsNullOrEmpty(_identifier))
            parts.Add(_identifier);
        else
            parts.Add("*");

        if (!string.IsNullOrEmpty(_variant))
            parts.Add(_variant);
        else if (string.IsNullOrEmpty(_identifier))
            parts.Add("*");

        return string.Join(Separator, parts);
    }

    /// <summary>
    /// Normalizes cache key components (lowercase, no special chars).
    /// </summary>
    private static string Normalize(string input)
    {
        return input.ToLowerInvariant().Trim();
    }
}

/// <summary>
/// Factory methods for common reservation cache keys.
/// 
/// Provides strongly-typed, discoverable cache key generation.
/// Reduces typos and ensures consistency.
/// 
/// Usage:
/// <code>
/// var key = ReservationCacheKeys.CustomerReservations(customerId);
/// var pattern = ReservationCacheKeys.AllCustomerReservationsPattern();
/// </code>
/// </summary>
public static class ReservationCacheKeys
{
    /// <summary>
    /// Cache key for customer's reservation list.
    /// Pattern: reservations:customer:{customerId}
    /// </summary>
    public static string CustomerReservations(Guid customerId) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("customer")
            .WithId(customerId)
            .Build();

    /// <summary>
    /// Pattern for all customer reservation caches.
    /// Useful for bulk invalidation when a reservation changes.
    /// </summary>
    public static string AllCustomerReservationsPattern() =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("customer")
            .BuildPattern();

    /// <summary>
    /// Cache key for reservation availability in a date range.
    /// Pattern: reservations:daterange:{startTicks}:{endTicks}
    /// </summary>
    public static string DateRangeAvailability(DateTime startDate, DateTime endDate) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("daterange")
            .WithDateRange(startDate, endDate)
            .Build();

    /// <summary>
    /// Pattern for all date range availability caches.
    /// Useful for bulk invalidation.
    /// </summary>
    public static string AllDateRangeAvailabilityPattern() =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("daterange")
            .BuildPattern();

    /// <summary>
    /// Cache key for count of active reservations by customer.
    /// Pattern: reservations:count:active:{customerId}
    /// </summary>
    public static string ActiveReservationCount(Guid customerId) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("count")
            .WithVariant("active")
            .WithId(customerId)
            .Build();

    /// <summary>
    /// Pattern for all active reservation count caches.
    /// Useful for bulk invalidation.
    /// </summary>
    public static string AllActiveReservationCountPattern() =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("count")
            .WithVariant("active")
            .BuildPattern();

    /// <summary>
    /// Cache key for a single reservation by ID.
    /// Pattern: reservations:byid:{reservationId}
    /// 
    /// Usage for GET endpoints that retrieve individual reservations.
    /// Invalidated when the reservation is created, confirmed, or cancelled.
    /// </summary>
    public static string ReservationById(Guid reservationId) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("byid")
            .WithId(reservationId)
            .Build();

    /// <summary>
    /// Pattern for all individual reservation caches.
    /// Useful for bulk invalidation when reservation state changes.
    /// </summary>
    public static string AllReservationByIdPattern() =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("byid")
            .BuildPattern();

    /// <summary>
    /// Cache key for reservations filtered by customer and status.
    /// Pattern: reservations:bystatus:{customerId}:{status}
    /// 
    /// Usage for queries like "Get my pending reservations" or "Get my confirmed bookings".
    /// Invalidated when reservation status changes for that customer.
    /// </summary>
    public static string ReservationsByCustomerAndStatus(Guid customerId, string status) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("bystatus")
            .WithVariant(status)
            .WithId(customerId)
            .Build();

    /// <summary>
    /// Pattern for all status-filtered reservation caches.
    /// Useful for invalidating status-specific views.
    /// </summary>
    public static string AllReservationsByStatusPattern(string status) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("bystatus")
            .WithVariant(status)
            .BuildPattern();

    /// <summary>
    /// Pattern for all status-filtered reservations across all customers.
    /// Useful for comprehensive status-based invalidation.
    /// </summary>
    public static string AllReservationsByStatusPattern() =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("bystatus")
            .BuildPattern();

    /// <summary>
    /// Cache key for customer's reservation summary/statistics.
    /// Pattern: reservations:summary:{customerId}
    /// 
    /// Usage for dashboard views showing reservation stats.
    /// Invalidated when any reservation changes for that customer.
    /// </summary>
    public static string CustomerReservationSummary(Guid customerId) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("summary")
            .WithId(customerId)
            .Build();

    /// <summary>
    /// Pattern for all customer summary caches.
    /// Useful for invalidating dashboard data.
    /// </summary>
    public static string AllCustomerReservationSummaryPattern() =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("summary")
            .BuildPattern();

    /// <summary>
    /// Cache key for pagination of customer's reservations.
    /// Pattern: reservations:paginated:{customerId}:{pageNumber}:{pageSize}
    /// 
    /// Usage for paginated list endpoints with customer filtering.
    /// Invalidated when customer's reservations change.
    /// </summary>
    public static string PaginatedCustomerReservations(Guid customerId, int pageNumber, int pageSize) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("paginated")
            .WithId($"{customerId}:{pageNumber}:{pageSize}")
            .Build();

    /// <summary>
    /// Pattern for all paginated reservation caches for a customer.
    /// Useful for invalidating all pages when list changes.
    /// </summary>
    public static string AllPaginatedCustomerReservationsPattern(Guid customerId) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("paginated")
            .WithId(customerId.ToString())
            .BuildPattern();

    /// <summary>
    /// Pattern for all reservation-related caches (comprehensive invalidation).
    /// Use only when you need to clear entire reservation cache subsystem.
    /// </summary>
    public static string AllReservationCachesPattern() =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .BuildPattern();

    /// <summary>
    /// Cache key for reservation conflict check result (use sparingly - not recommended).
    /// Pattern: reservations:conflicts:{startDateTicks}:{endDateTicks}
    /// 
    /// ⚠️ WARNING: Conflict checks should generally NOT be cached due to consistency risks.
    /// Only use if you implement strict invalidation and understand the trade-offs.
    /// Recommended: Always query database directly for conflict detection.
    /// </summary>
    [Obsolete("Conflict detection should generally NOT be cached. Query database directly to prevent overbooking.")]
    public static string ReservationConflicts(DateTime startDate, DateTime endDate) =>
        CacheKeyBuilder
            .ForFeature("reservations")
            .ForResource("conflicts")
            .WithDateRange(startDate, endDate)
            .Build();
}
