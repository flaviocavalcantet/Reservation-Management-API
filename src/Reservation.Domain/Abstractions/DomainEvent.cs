namespace Reservation.Domain.Abstractions;

/// <summary>
/// Base class for domain events. Used in DDD to represent something that happened in the domain.
/// Supports event sourcing patterns and enables cross-aggregate communication.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Unique identifier for this event occurrence
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
