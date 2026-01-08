namespace Reservation.Domain.Abstractions;

/// <summary>
/// Unit of Work pattern interface. Coordinates the use of repositories and ensures
/// atomic operations across multiple aggregates. In .NET with EF Core, DbContext
/// naturally implements this pattern.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Persists all changes made to aggregates in this unit of work
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction for atomic operations across multiple aggregates
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
