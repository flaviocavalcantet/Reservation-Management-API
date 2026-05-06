using Reservation.Domain.Abstractions;

namespace Reservation.Application.Events;

/// <summary>
/// Base class for externalized domain events intended for publishing to message brokers.
/// These are serializable representations of domain events that cross aggregate boundaries.
/// 
/// Design principles:
/// - Immutable: Once created, events cannot be modified
/// - Serializable: Can be converted to JSON for message brokers
/// - Self-contained: Include all necessary data for event consumers
/// - Correlation aware: Include metadata for distributed tracing
/// </summary>
public abstract class IntegrationEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred (UTC)
    /// </summary>
    public DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Correlation ID for distributed tracing and request correlation
    /// Allows tracking related events and operations across services
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Causation ID to track what caused this event
    /// Links this event to the command that triggered it
    /// </summary>
    public Guid CausationId { get; }

    /// <summary>
    /// Version of the event schema for handling schema evolution
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Initializes a new instance of the IntegrationEvent class.
    /// </summary>
    /// <param name="correlationId">Correlation ID for distributed tracing</param>
    /// <param name="causationId">ID of the command that caused this event</param>
    protected IntegrationEvent(Guid correlationId = default, Guid causationId = default)
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
        CorrelationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId;
        CausationId = causationId == Guid.Empty ? Guid.NewGuid() : causationId;
        Version = 1;
    }

    /// <summary>
    /// Gets the event type name for serialization and routing
    /// </summary>
    public virtual string EventType => GetType().Name;

    /// <summary>
    /// Gets the topic/channel this event should be published to
    /// Override in derived classes to customize routing
    /// </summary>
    public virtual string Topic => "events";

    /// <summary> Gets the message key for partitioning in Kafka
    /// Default is EventId, but can be overridden for different partitioning strategies
    /// </summary>
    public virtual string Key => EventId.ToString();
}
