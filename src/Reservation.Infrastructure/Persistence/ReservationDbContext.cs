using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Abstractions;

namespace Reservation.Infrastructure.Persistence;

/// <summary>
/// Database context for the Reservation Management System.
/// Manages entities, change tracking, and persistence operations using Entity Framework Core.
/// Implements the Unit of Work pattern through DbContext lifecycle management.
/// Configured for PostgreSQL with optimizations for production scenarios.
/// </summary>
public class ReservationDbContext : DbContext, IUnitOfWork
{
    // DbSets will be added here as aggregate roots are implemented
    // Example: public DbSet<Reservation> Reservations { get; set; }

    public ReservationDbContext(DbContextOptions<ReservationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the model and entity mappings. Override this to define:
    /// - Entity configurations
    /// - Relationships
    /// - Value object mappings
    /// - Indexes and constraints
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReservationDbContext).Assembly);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Database.RollbackTransactionAsync(cancellationToken);
        }
        finally
        {
            await Database.CloseConnectionAsync();
        }
    }
}
