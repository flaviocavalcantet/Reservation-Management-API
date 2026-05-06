using Microsoft.Extensions.Logging;
using Reservation.Application.Events;

namespace Reservation.Infrastructure.Messaging;

/// <summary>
/// In-memory implementation of IEventPublisher for development and testing.
/// Stores events in memory instead of publishing to a message broker.
/// Works with both DomainEvent and IntegrationEvent types.
/// 
/// Use cases:
/// - Development without Kafka infrastructure
/// - Unit testing
/// - Integration testing with controlled event flow
/// - Debugging event publishing logic
/// </summary>
public class InMemoryEventPublisher : IEventPublisher
{
    private readonly List<object> _publishedEvents = new();
    private readonly ILogger<InMemoryEventPublisher> _logger;

    /// <summary>
    /// Gets the collection of all published events.
    /// Useful for testing and verification.
    /// </summary>
    public IReadOnlyList<object> PublishedEvents => _publishedEvents.AsReadOnly();

    /// <summary>
    /// Initializes a new instance of the InMemoryEventPublisher class.
    /// </summary>
    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes an event to the in-memory store.
    /// </summary>
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _publishedEvents.Add(@event);
        
        _logger.LogInformation(
            "Event published to in-memory store. EventType: {EventType}, EventCount: {EventCount}",
            @event.GetType().Name,
            _publishedEvents.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes multiple events to the in-memory store.
    /// </summary>
    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) where TEvent : class
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventList = events.ToList();

        foreach (var @event in eventList)
        {
            await PublishAsync(@event, cancellationToken);
        }

        _logger.LogInformation(
            "Batch publishing completed in-memory. EventCount: {EventCount}",
            eventList.Count);
    }

    /// <summary>
    /// Clears all published events from the in-memory store.
    /// Useful for test cleanup.
    /// </summary>
    public void Clear()
    {
        _publishedEvents.Clear();
    }

    /// <summary>
    /// Gets all published events of a specific type.
    /// </summary>
    public IEnumerable<T> GetPublishedEvents<T>() where T : class
    {
        return _publishedEvents.OfType<T>();
    }
}
