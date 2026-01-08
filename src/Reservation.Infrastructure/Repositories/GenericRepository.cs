using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Abstractions;
using Reservation.Infrastructure.Persistence;

namespace Reservation.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation providing standard CRUD operations.
/// Uses EF Core DbContext for data access and respects aggregate boundaries.
/// Supports async operations for scalability and proper database resource management.
/// 
/// Note: Since AggregateRoot always has Guid Id, TId should be Guid.
/// EF.Property is used for type-safe generic comparisons in LINQ queries.
/// </summary>
public class GenericRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot
{
    protected readonly ReservationDbContext DbContext;
    protected readonly DbSet<TAggregate> DbSet;

    public GenericRepository(ReservationDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TAggregate>();
    }

    public virtual async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(aggregate, cancellationToken);
    }

    /// <summary>
    /// Retrieves an aggregate by its identifier.
    /// Uses EF.Property for type-safe comparison in LINQ expressions.
    /// </summary>
    public virtual async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        // EF.Property allows generic property access without dynamic operations
        return await DbSet.FirstOrDefaultAsync(
            x => EF.Property<Guid>(x, nameof(Entity.Id)).Equals(id),
            cancellationToken);
    }

    public virtual async Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        DbSet.Update(aggregate);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(aggregate);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if an aggregate with the given identifier exists.
    /// Uses EF.Property for type-safe comparison in LINQ expressions.
    /// </summary>
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        // EF.Property allows generic property access without dynamic operations
        return await DbSet.AnyAsync(
            x => EF.Property<Guid>(x, nameof(Entity.Id)).Equals(id),
            cancellationToken);
    }
}
