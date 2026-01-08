using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Abstractions;
using Reservation.Domain.Reservations;
using Reservation.Infrastructure.Persistence;

namespace Reservation.Infrastructure.Repositories;

/// <summary>
/// Concrete repository implementation for Reservation aggregate.
/// 
/// Extends GenericRepository to provide:
/// - Standard CRUD operations (from base class)
/// - Specialized queries specific to Reservation domain
/// 
/// Responsibilities:
/// - Translate domain queries into EF Core operations
/// - Enforce access patterns via IReservationRepository interface
/// - Hide EF Core implementation from Application layer
/// </summary>
public class ReservationRepository : GenericRepository<Domain.Reservations.Reservation, Guid>, IReservationRepository
{
    public ReservationRepository(ReservationDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// Retrieves all reservations for a specific customer.
    /// 
    /// Query Pattern:
    /// - Filters by CustomerId
    /// - Orders by StartDate descending (most recent first)
    /// - Returns all reservation states (Created, Confirmed, Cancelled)
    /// 
    /// Use Case: GetReservations query handler, customer history view
    /// </summary>
    public async Task<IEnumerable<Domain.Reservations.Reservation>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves reservations within a date range.
    /// 
    /// Query Pattern:
    /// - Filters by start/end dates
    /// - Optimizes with index: idx_reservations_dates
    /// - Returns non-cancelled reservations only
    /// 
    /// Use Case: Availability checking, booking conflicts
    /// </summary>
    public async Task<IEnumerable<Domain.Reservations.Reservation>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.StartDate <= endDate && 
                       r.EndDate >= startDate &&
                       !r.Status.IsCancelled)  // Exclude cancelled reservations
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds confirmed reservations that conflict with a given date range.
    /// Critical for preventing double-bookings.
    /// </summary>
    public async Task<IEnumerable<Domain.Reservations.Reservation>> GetConflictingReservationsAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.StartDate <= endDate &&
                       r.EndDate >= startDate &&
                       !r.Status.IsCancelled &&
                       (excludeReservationId == null || r.Id != excludeReservationId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts active (confirmed) reservations for a customer.
    /// 
    /// Query Pattern:
    /// - Only counts confirmed reservations
    /// - Excludes created and cancelled states
    /// 
    /// Use Case: Quota enforcement, VIP customer limits, analytics
    /// </summary>
    public async Task<int> CountActiveByCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.CustomerId == customerId &&
                       r.Status.IsConfirmed)  // Only confirmed
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves active (confirmed) reservations for a customer within a date range.
    /// 
    /// Combines:
    /// - Customer filtering
    /// - Date range filtering
    /// - Active status only
    /// 
    /// Use Case: Schedule view, capacity planning
    /// </summary>
    public async Task<IEnumerable<Domain.Reservations.Reservation>> GetActiveByCustomerAndDateRangeAsync(
        Guid customerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.CustomerId == customerId &&
                       r.StartDate <= endDate &&
                       r.EndDate >= startDate &&
                       r.Status.IsConfirmed)  // Only confirmed
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);
    }
}
