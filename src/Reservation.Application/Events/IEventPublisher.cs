using Reservation.Domain.Abstractions;

namespace Reservation.Application.Events;

/// <summary>
/// Abstraction for publishing domain and integration events.
/// This abstraction decouples the application layer from any specific messaging implementation.
/// Enables event-driven architecture while maintaining Clean Architecture principles.
/// 
/// The interface is generic to support both domain events (from domain layer) and
/// integration events (for cross-service communication via Kafka).
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single event to the messaging infrastructure.
    /// Works with both DomainEvent and IntegrationEvent types.
    /// Implementation handles serialization, routing, and delivery guarantees.
    /// </summary>
    /// <typeparam name="TEvent">Event type to publish</typeparam>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous publication operation</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;

    /// <summary>
    /// Publishes multiple events in order.
    /// Events are published sequentially to maintain ordering guarantees.
    /// </summary>
    /// <typeparam name="TEvent">Event type to publish</typeparam>
    /// <param name="events">Collection of events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous publication operation</returns>
    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) where TEvent : class;
}
