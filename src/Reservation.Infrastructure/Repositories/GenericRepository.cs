using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Abstractions;
using Reservation.Infrastructure.Persistence;

namespace Reservation.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation with specification support.
/// Provides standard CRUD operations and specification-based queries.
/// Uses EF Core DbContext for data access and respects aggregate boundaries.
/// 
/// Pattern: Generic repository with specification pattern
/// - Separates data access logic from domain logic
/// - Enables query reuse through specifications
/// - Supports both generic and specialized queries
/// - Maintains single responsibility per repository
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

    /// <summary>
    /// Gets aggregates matching a specification
    /// </summary>
    protected virtual async Task<IEnumerable<TAggregate>> GetBySpecificationAsync(
        Specification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the first aggregate matching a specification
    /// </summary>
    protected virtual async Task<TAggregate?> GetFirstBySpecificationAsync(
        Specification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Counts aggregates matching a specification
    /// </summary>
    protected virtual async Task<int> CountBySpecificationAsync(
        Specification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Applies a specification to the query
    /// </summary>
    protected virtual IQueryable<TAggregate> ApplySpecification(Specification<TAggregate> specification)
    {
        var query = DbSet.AsQueryable();

        // Apply criteria
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Apply string-based includes for collections
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderByExpression != null)
        {
            query = specification.IsOrderByDescending
                ? query.OrderByDescending(specification.OrderByExpression)
                : query.OrderBy(specification.OrderByExpression);
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }
        }

        return query;
    }
}
