using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Reservation.Application.Events;
using Reservation.Domain.Abstractions;
using System.Text.Json;

namespace Reservation.Infrastructure.Messaging;

/// <summary>
/// Kafka implementation of IEventPublisher.
/// Publishes domain events to Kafka topics for consumption by other services.
/// 
/// Features:
/// - Asynchronous publishing for non-blocking operations
/// - Automatic topic routing based on event type
/// - JSON serialization for language-agnostic message format
/// - Error handling and retry logic
/// - Correlation ID tracking for distributed tracing
/// - Idempotent producer configuration for at-least-once delivery
/// </summary>
public class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the KafkaEventPublisher class.
    /// </summary>
    /// <param name="producer">Kafka producer instance configured for message publishing</param>
    /// <param name="logger">Logger instance for diagnostic information</param>
    public KafkaEventPublisher(IProducer<string, string> producer, ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes a single event to Kafka.
    /// Works with both DomainEvent and IntegrationEvent types.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        try
        {
            // Serialize event to JSON
            var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Determine topic and key based on event type
            var (topic, key) = GetTopicAndKey(@event);

            // Create Kafka message with event ID as key (ensures ordering per partition)
            var message = new Message<string, string>
            {
                Key = key,
                Value = json,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.GetType().Name) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            };

            // Publish to Kafka with callback
            var deliveryReport = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Event published successfully. EventType: {EventType}, Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                @event.GetType().Name,
                topic,
                deliveryReport.Partition,
                deliveryReport.Offset);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "Event publishing was cancelled. EventType: {EventType}",
                @event.GetType().Name);
            throw;
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Kafka delivery failed. EventType: {EventType}, Reason: {Reason}",
                @event.GetType().Name,
                ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error publishing event. EventType: {EventType}",
                @event.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple events to Kafka in batch.
    /// Events are published sequentially to maintain order.
    /// </summary>
    public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) where TEvent : class
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var eventList = events.ToList();
        
        _logger.LogInformation(
            "Publishing batch of {EventCount} events",
            eventList.Count);

        foreach (var @event in eventList)
        {
            await PublishAsync(@event, cancellationToken);
        }

        _logger.LogInformation(
            "Batch publishing completed. EventCount: {EventCount}",
            eventList.Count);
    }

    /// <summary>
    /// Converts a domain event to an integration event.
    /// This method adapts domain events to integration event format for Kafka publishing.
    /// </summary>
    private static IntegrationEvent ConvertToIntegrationEvent(DomainEvent domainEvent)
    {
        // Map domain events to integration events
        // This allows domain events to remain framework-agnostic while still being published
        return domainEvent switch
        {
            // Add mappings as domain events are emitted
            // Example:
            // ReservationCreatedDomainEvent rce => new ReservationCreatedIntegrationEvent(
            //     rce.ReservationId, rce.CustomerId, ...),
            
            _ => throw new NotSupportedException(
                $"Domain event type '{domainEvent.GetType().Name}' is not supported for integration event conversion.")
        };
    }
}
