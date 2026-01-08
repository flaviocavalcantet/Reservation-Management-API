namespace Reservation.Domain.Abstractions;

/// <summary>
/// Marker interface for repository abstractions. Used to identify repository implementations
/// in the infrastructure layer. Enables loose coupling between domain and persistence logic.
/// </summary>
public interface IRepository
{
}

/// <summary>
/// Generic repository interface for aggregate persistence. Provides data access abstraction
/// for aggregate roots. Implementation should enforce aggregate boundaries and consistency.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type</typeparam>
/// <typeparam name="TId">The primary key type of the aggregate</typeparam>
public interface IRepository<TAggregate, TId> : IRepository
    where TAggregate : AggregateRoot
{
    /// <summary>
    /// Adds a new aggregate to the repository
    /// </summary>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an aggregate by its identifier
    /// </summary>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate
    /// </summary>
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an aggregate
    /// </summary>
    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an aggregate exists
    /// </summary>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);
}
