using MediatR;
using Reservation.Domain.Abstractions;

namespace Reservation.Application.Abstractions;

/// <summary>
/// Handles publishing domain events to subscribers after successful command execution.
/// This ensures that side effects and cross-aggregate consistency are properly managed.
/// Implements the Event Publishing pattern from DDD.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes all domain events from an aggregate
    /// </summary>
    Task PublishAsync(AggregateRoot aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple domain events
    /// </summary>
    Task PublishAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);
}
