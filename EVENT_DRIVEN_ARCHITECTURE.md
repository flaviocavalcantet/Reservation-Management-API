# Event-Driven Architecture with Kafka

Complete guide to implementing event-driven messaging in the Reservation Management API using Apache Kafka.

**Status**: ✅ Production-Ready Design Pattern  
**Clean Architecture**: ✅ Fully Compliant  
**Framework Coupling**: ✅ Zero Kafka Dependencies in Domain/Application

---

## Overview

This architecture enables event-driven communication between services while maintaining Clean Architecture principles. Domain events are decoupled from the messaging infrastructure through carefully designed abstractions.

### Key Benefits

- **Decoupling**: Services communicate asynchronously without direct dependencies
- **Scalability**: Event-driven pattern supports high-throughput, low-latency scenarios
- **Resilience**: Kafka's durability ensures no events are lost
- **Auditability**: Complete event log provides comprehensive audit trail
- **Extensibility**: New consumers can subscribe to events without code changes

---

## Architecture

### Layered Design

```
┌─────────────────────────────────────────────────┐
│ API Layer (Endpoints)                           │
│ - Executes commands that trigger domain events  │
└──────────┬──────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────┐
│ Application Layer (Use Cases)                   │
│ - IEventPublisher abstraction (interface)       │
│ - Command handlers publish domain events        │
│ - No Kafka dependency                           │
└──────────┬──────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────┐
│ Domain Layer (Business Rules)                   │
│ - Aggregate roots emit domain events            │
│ - Events represent state changes                │
│ - No framework dependencies                     │
└──────────┬──────────────────────────────────────┘
           │
┌──────────▼──────────────────────────────────────┐
│ Infrastructure Layer (Messaging)                │
│ - KafkaEventPublisher implements IEventPublisher
│ - InMemoryEventPublisher for testing            │
│ - Kafka configuration and serialization         │
│ - Only layer with Kafka dependency              │
└─────────────────────────────────────────────────┘
```

### Domain Events vs Integration Events

| Aspect | Domain Event | Integration Event |
|--------|---|---|
| **Location** | Domain Layer | Application Layer |
| **Scope** | Intra-aggregate | Cross-service |
| **Audience** | Internal domain logic | External systems |
| **Serialization** | Not required | JSON (Kafka) |
| **Naming** | ReservationCreated | ReservationCreatedIntegrationEvent |
| **Coupling** | Zero to infrastructure | Zero to Kafka |

---

## Event Types

### 1. ReservationCreatedIntegrationEvent

**Topic**: `reservations.created`

Published when a new reservation is created.

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "eventType": "ReservationCreatedIntegrationEvent",
  "occuredOnUtc": "2026-01-24T10:30:00Z",
  "correlationId": "660f9511-f40c-52e5-b827-557766551111",
  "causationId": "770g0612-g51d-63f6-c938-668877662222",
  "version": 1,
  "reservationId": "880h0713-h62e-74g7-d049-779988773333",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "startDateUtc": "2026-02-15T14:00:00Z",
  "endDateUtc": "2026-02-20T10:00:00Z",
  "createdAtUtc": "2026-01-24T10:30:00Z"
}
```

**Consumers**:
- Email service: Send booking confirmation
- Analytics: Track booking trends
- Inventory: Reserve capacity
- Audit: Record creation event

### 2. ReservationConfirmedIntegrationEvent

**Topic**: `reservations.confirmed`

Published when a reservation is confirmed.

```json
{
  "eventId": "990i0814-i73f-85h8-e150-880099884444",
  "eventType": "ReservationConfirmedIntegrationEvent",
  "occuredOnUtc": "2026-01-24T11:15:00Z",
  "correlationId": "660f9511-f40c-52e5-b827-557766551111",
  "causationId": "aa0j0915-j84g-96i9-f261-991100995555",
  "version": 1,
  "reservationId": "880h0713-h62e-74g7-d049-779988773333",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "startDateUtc": "2026-02-15T14:00:00Z",
  "endDateUtc": "2026-02-20T10:00:00Z",
  "confirmedAtUtc": "2026-01-24T11:15:00Z"
}
```

**Consumers**:
- Email service: Send confirmation
- Payment service: Charge customer
- Inventory: Lock dates
- Audit: Record confirmation

### 3. ReservationCancelledIntegrationEvent

**Topic**: `reservations.cancelled`

Published when a reservation is cancelled.

```json
{
  "eventId": "bb0k1016-k95h-a7j0-g372-aa221100aa666",
  "eventType": "ReservationCancelledIntegrationEvent",
  "occuredOnUtc": "2026-01-24T12:00:00Z",
  "correlationId": "660f9511-f40c-52e5-b827-557766551111",
  "causationId": "cc0l1117-l06i-b8k1-h483-bb332211bb777",
  "version": 1,
  "reservationId": "880h0713-h62e-74g7-d049-779988773333",
  "customerId": "550e8400-e29b-41d4-a716-446655440000",
  "startDateUtc": "2026-02-15T14:00:00Z",
  "endDateUtc": "2026-02-20T10:00:00Z",
  "cancelledAtUtc": "2026-01-24T12:00:00Z",
  "cancellationReason": "Customer requested cancellation"
}
```

**Consumers**:
- Email service: Send cancellation confirmation
- Refund service: Process refund
- Inventory: Release dates
- Audit: Record cancellation with reason

---

## Implementation Guide

### 1. Configure Kafka Settings

**appsettings.Development.json**:
```json
{
  "Kafka": {
    "Enabled": false,
    "BootstrapServers": "localhost:9092",
    "GroupId": "reservation-api",
    "Acks": "all",
    "EnableIdempotence": true,
    "Retries": 3,
    "RetryBackoffMs": 100,
    "CompressionType": "snappy"
  }
}
```

**appsettings.Production.json**:
```json
{
  "Kafka": {
    "Enabled": true,
    "BootstrapServers": "kafka1:9092,kafka2:9092,kafka3:9092",
    "GroupId": "reservation-api",
    "Acks": "all",
    "EnableIdempotence": true,
    "SaslEnabled": true,
    "SaslMechanism": "SCRAM-SHA-512",
    "SaslUsername": "${KAFKA_USERNAME}",
    "SaslPassword": "${KAFKA_PASSWORD}",
    "SslEnabled": true,
    "SslCaLocation": "/etc/kafka/certs/ca.crt"
  }
}
```

### 2. Register Messaging Services

**Program.cs**:
```csharp
// Add messaging (Kafka or in-memory)
builder.Services.AddMessaging(builder.Configuration);

// Now IEventPublisher is available for injection
```

### 3. Emit Domain Events

Update your domain entities to emit events:

```csharp
public class Reservation : AggregateRoot
{
    public static Reservation Create(
        Guid customerId,
        DateTime startDate,
        DateTime endDate)
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            Status = ReservationStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        // Emit domain event
        reservation.AddDomainEvent(new ReservationCreatedDomainEvent(
            reservation.Id,
            customerId,
            startDate,
            endDate));

        return reservation;
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Created)
        {
            throw new InvalidOperationException("Only created reservations can be confirmed");
        }

        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;

        // Emit domain event
        AddDomainEvent(new ReservationConfirmedDomainEvent(
            Id,
            CustomerId,
            StartDate,
            EndDate,
            ConfirmedAt.Value));
    }

    public void Cancel(string? reason = null)
    {
        // Business rules...

        Status = ReservationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        // Emit domain event
        AddDomainEvent(new ReservationCancelledDomainEvent(
            Id,
            CustomerId,
            StartDate,
            EndDate,
            CancelledAt.Value,
            reason));
    }
}
```

### 4. Publish Events from Application Layer

**CreateReservationCommandHandler.cs**:
```csharp
public class CreateReservationCommandHandler : ICommandHandler<CreateReservationCommand, Guid>
{
    private readonly IReservationRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CreateReservationCommandHandler> _logger;

    public async Task<Guid> Handle(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        // Create reservation (emits domain event)
        var reservation = Reservation.Create(
            command.CustomerId,
            command.StartDate,
            command.EndDate);

        // Save to repository
        await _repository.AddAsync(reservation, cancellationToken);
        await _repository.SaveAsync(cancellationToken);

        // Publish domain events to message broker
        var domainEvents = reservation.GetAndClearDomainEvents();
        foreach (var @event in domainEvents)
        {
            await _eventPublisher.PublishAsync(@event, cancellationToken);
        }

        _logger.LogInformation(
            "Reservation created and events published. ReservationId: {ReservationId}",
            reservation.Id);

        return reservation.Id;
    }
}
```

---

## Kafka Configuration Details

### Producer Configuration

**Durability Settings** (acks=all):
- All in-sync replicas must acknowledge
- Guarantees no data loss even if broker fails
- Slightly higher latency trade-off

**Idempotence** (enableIdempotence=true):
- Ensures each message produced exactly once
- Prevents duplicate events in Kafka
- Requires: Acks=all, Retries>0, MaxInFlightRequests≤5

**Compression** (snappy):
- Reduces network bandwidth
- CPU-efficient compression algorithm
- Trade-off: CPU for network savings

### Kafka Architecture for Production

```
┌──────────────────────┐
│ Reservation API      │
│ (Producer)           │
└──────────┬───────────┘
           │
        Events
           │
┌──────────▼───────────────────────────────────┐
│ Apache Kafka Cluster (3+ brokers)            │
├───────────────────────────────────────────────┤
│ Topic: reservations.created (3 partitions)    │
│ Topic: reservations.confirmed (3 partitions)  │
│ Topic: reservations.cancelled (3 partitions)  │
│ Replication Factor: 3                         │
│ Min ISR: 2                                    │
└──────────┬──────────────────────────────────┘
           │
    ┌──────┼──────────┐
    │      │          │
    ▼      ▼          ▼
┌────────┐┌─────────┐┌──────────┐
│ Email  ││ Inventory││ Refund   │
│Service ││ Service  ││ Service  │
└────────┘└─────────┘└──────────┘
(Consumers)
```

### Partition Strategy

**Events by ReservationId (Partitioning Key)**:
```
Message: { Key: "880h0713-...", Value: {...event...} }
```

- All events for same reservation go to same partition
- Maintains ordering per reservation
- Enables parallel processing across partitions
- Consumers can scale independently

---

## Testing with Events

### Unit Testing with InMemoryEventPublisher

```csharp
[Fact]
public async Task CreateReservation_PublishesReservationCreatedEvent()
{
    // Arrange
    var eventPublisher = new InMemoryEventPublisher(Logger);
    var repository = new InMemoryReservationRepository();
    var handler = new CreateReservationCommandHandler(
        repository,
        eventPublisher,
        Logger);

    var command = new CreateReservationCommand(
        CustomerId: Guid.NewGuid(),
        StartDate: DateTime.UtcNow.AddDays(5),
        EndDate: DateTime.UtcNow.AddDays(10));

    // Act
    var reservationId = await handler.Handle(command, CancellationToken.None);

    // Assert
    var publishedEvents = eventPublisher.GetPublishedEvents<ReservationCreatedDomainEvent>();
    Assert.Single(publishedEvents);
    Assert.Equal(reservationId, publishedEvents.First().ReservationId);
}
```

### Integration Testing with Test Containers

```csharp
[Collection("Kafka Collection")]
public class ReservationIntegrationTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafkaContainer;

    public async Task InitializeAsync()
    {
        _kafkaContainer = new KafkaContainer(DockerImage.Image("confluentinc/cp-kafka:7.5.0"));
        await _kafkaContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _kafkaContainer.StopAsync();
    }

    [Fact]
    public async Task CreateReservation_ProducesToKafka()
    {
        // Arrange
        var bootstrapServers = _kafkaContainer.GetBootstrapAddress();
        // ... configure Kafka with bootstrap servers ...

        // Act
        var reservationId = await handler.Handle(command, CancellationToken.None);

        // Assert
        // Consume from Kafka and verify event
    }
}
```

---

## Error Handling & Resilience

### Automatic Retries

The Kafka producer automatically retries transient failures:
- Network timeouts
- Broker unavailability
- Temporary connection issues

**Configuration**:
```json
{
  "Retries": 3,
  "RetryBackoffMs": 100
}
```

### Circuit Breaker Pattern

Add circuit breaker for Kafka failures:

```csharp
services.AddResiliencePipeline("kafka-publisher", builder =>
{
    builder
        .AddTimeout(TimeSpan.FromSeconds(10))
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(100)
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 4
        });
});
```

### Dead Letter Handling

For events that fail to publish:

```csharp
public class DeadLetterEventStore : IEventPublisher
{
    private readonly IEventPublisher _innerPublisher;
    private readonly IDeadLetterStore _deadLetterStore;

    public async Task PublishAsync(DomainEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            await _innerPublisher.PublishAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event, storing in dead letter queue");
            await _deadLetterStore.StoreAsync(@event, ex, cancellationToken);
        }
    }
}
```

---

## Monitoring & Observability

### Key Metrics

```csharp
public class KafkaMetrics
{
    // Counters
    public static Counter<int> EventsPublished { get; } =
        new("kafka.events.published", "Number of events published");

    public static Counter<int> EventsPublishedFailed { get; } =
        new("kafka.events.published.failed", "Number of failed event publications");

    // Histograms
    public static Histogram<double> PublishDuration { get; } =
        new("kafka.events.publish.duration", "Time taken to publish an event (ms)");

    // Gauges
    public static ObservableGauge<long> PendingMessages { get; } =
        new("kafka.producer.pending_messages", "Number of messages pending publish");
}
```

### Logging

```csharp
_logger.LogInformation(
    "Event published. EventType: {EventType}, EventId: {EventId}, " +
    "Topic: {Topic}, Partition: {Partition}, CorrelationId: {CorrelationId}",
    @event.GetType().Name,
    eventId,
    topic,
    partition,
    correlationId);
```

### Health Checks

```csharp
services.AddHealthChecks()
    .AddKafka(options =>
    {
        options.BootstrapServers = kafkaSettings.BootstrapServers;
    })
    .AddCheck("KafkaProducer", async () =>
    {
        try
        {
            await eventPublisher.PublishAsync(
                new HealthCheckEvent(),
                CancellationToken.None);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Failed to publish to Kafka", ex);
        }
    });
```

---

## Disaster Recovery

### Kafka Cluster Failure Handling

1. **Producer-side buffering**: Events buffer in memory until broker recovers
2. **Automatic failover**: Redirects to healthy brokers
3. **Idempotent producers**: Prevents duplicates on failover

### Data Loss Prevention

1. **Acks=all**: Waits for all replicas
2. **Replication factor 3**: Survives 2 broker failures
3. **Min ISR=2**: Ensures durability
4. **Event sourcing**: Maintains event history

---

## Migration Guide

### Phase 1: Development (In-Memory)
- Use InMemoryEventPublisher
- No Kafka infrastructure needed
- Full development velocity

### Phase 2: Staging (Local Kafka)
- Docker Compose Kafka cluster
- Integration testing
- Performance validation

### Phase 3: Production (Managed Kafka)
- AWS MSK, Confluent Cloud, or self-hosted
- Enable SASL/SSL
- Configure monitoring and alerting

---

## Best Practices

1. **Event Naming**: Use past tense (ReservationCreated, not CreatingReservation)
2. **Event Data**: Include all context needed by consumers
3. **Immutability**: Events cannot be modified after publishing
4. **Idempotency**: Design consumers to handle duplicate events
5. **Schema Versioning**: Support event schema evolution
6. **Correlation IDs**: Track events across services
7. **Ordering**: Use partition key to guarantee order per entity
8. **Monitoring**: Track publishing latency and failures

---

## Related Files

- **Integration Events**: [Application/Events/Reservations/](../src/Reservation.Application/Events/Reservations/)
- **Kafka Publisher**: [Infrastructure/Messaging/KafkaEventPublisher.cs](../src/Reservation.Infrastructure/Messaging/KafkaEventPublisher.cs)
- **Configuration**: [appsettings.json](../src/Reservation.API/appsettings.json)
- **Domain Events**: [Domain/Reservations/ReservationEvents.cs](../src/Reservation.Domain/Reservations/ReservationEvents.cs)

---

**Last Updated**: January 24, 2026
