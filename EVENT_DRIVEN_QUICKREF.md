# Event-Driven Architecture Quick Reference

Fast lookup for event-driven messaging patterns and code.

---

## Quick Links

| Need | Document |
|------|-----------|
| Full implementation guide | [EVENT_DRIVEN_ARCHITECTURE.md](EVENT_DRIVEN_ARCHITECTURE.md) |
| Implementation summary | [EVENT_DRIVEN_IMPLEMENTATION.md](EVENT_DRIVEN_IMPLEMENTATION.md) |
| API endpoints | [API_ENDPOINTS.md](API_ENDPOINTS.md) |
| Testing | [TESTING.md](TESTING.md) |

---

## File Locations

```
Events (Application Layer)
├── IEventPublisher.cs                      [Interface - no Kafka]
├── IntegrationEvent.cs                     [Base class]
└── Reservations/
    ├── ReservationCreatedIntegrationEvent.cs
    ├── ReservationConfirmedIntegrationEvent.cs
    └── ReservationCancelledIntegrationEvent.cs

Messaging (Infrastructure Layer)
├── KafkaEventPublisher.cs                  [Kafka producer]
├── InMemoryEventPublisher.cs               [Testing]
├── KafkaSettings.cs                        [Configuration]
└── MessagingServiceCollectionExtensions.cs [DI setup]
```

---

## Setup

### 1. Configure appsettings.json

**Development (In-Memory)**:
```json
{
  "Kafka": {
    "Enabled": false
  }
}
```

**Production (Kafka)**:
```json
{
  "Kafka": {
    "Enabled": true,
    "BootstrapServers": "kafka1:9092,kafka2:9092,kafka3:9092",
    "Acks": "all",
    "EnableIdempotence": true,
    "Retries": 3
  }
}
```

### 2. Register in Program.cs

```csharp
builder.Services.AddMessaging(builder.Configuration);
```

---

## Publishing Events

### Create Event

```csharp
var @event = new ReservationCreatedIntegrationEvent(
    reservationId: reservation.Id,
    customerId: reservation.CustomerId,
    startDateUtc: reservation.StartDate,
    endDateUtc: reservation.EndDate,
    createdAtUtc: reservation.CreatedAt,
    correlationId: request.CorrelationId,
    causationId: commandId);
```

### Publish Event

```csharp
await _eventPublisher.PublishAsync(@event, cancellationToken);
```

### Publish Batch

```csharp
var events = reservation.GetAndClearDomainEvents();
await _eventPublisher.PublishBatchAsync(events, cancellationToken);
```

---

## Event Contracts

### ReservationCreatedIntegrationEvent
```csharp
Topic: reservations.created

new ReservationCreatedIntegrationEvent(
    reservationId: Guid,
    customerId: Guid,
    startDateUtc: DateTime,
    endDateUtc: DateTime,
    createdAtUtc: DateTime)
```

### ReservationConfirmedIntegrationEvent
```csharp
Topic: reservations.confirmed

new ReservationConfirmedIntegrationEvent(
    reservationId: Guid,
    customerId: Guid,
    startDateUtc: DateTime,
    endDateUtc: DateTime,
    confirmedAtUtc: DateTime)
```

### ReservationCancelledIntegrationEvent
```csharp
Topic: reservations.cancelled

new ReservationCancelledIntegrationEvent(
    reservationId: Guid,
    customerId: Guid,
    startDateUtc: DateTime,
    endDateUtc: DateTime,
    cancelledAtUtc: DateTime,
    cancellationReason: string?)
```

---

## Testing

### With InMemoryEventPublisher

```csharp
[Fact]
public async Task CreateReservation_PublishesEvent()
{
    // Arrange
    var eventPublisher = new InMemoryEventPublisher(logger);
    var handler = new CreateReservationCommandHandler(
        repository,
        eventPublisher,
        logger);

    // Act
    var reservationId = await handler.Handle(command, CancellationToken.None);

    // Assert
    var publishedEvents = eventPublisher.GetPublishedEvents
        <ReservationCreatedIntegrationEvent>();
    Assert.Single(publishedEvents);
    Assert.Equal(reservationId, publishedEvents.First().ReservationId);
}
```

---

## Kafka Topics

| Topic | Event | Partition Key | Retention |
|-------|-------|---------------|-----------|
| `reservations.created` | ReservationCreatedIntegrationEvent | ReservationId | 7 days |
| `reservations.confirmed` | ReservationConfirmedIntegrationEvent | ReservationId | 7 days |
| `reservations.cancelled` | ReservationCancelledIntegrationEvent | ReservationId | 7 days |

---

## Monitoring

### Key Metrics

```csharp
// Events published successfully
metrics.Increment("kafka.events.published", 1);

// Publishing duration
metrics.Histogram("kafka.events.publish.duration", durationMs);

// Publishing failures
metrics.Increment("kafka.events.published.failed", 1);
```

### Health Check

```csharp
services.AddHealthChecks()
    .AddCheck("Kafka", async () =>
    {
        try
        {
            await eventPublisher.PublishAsync(
                new HealthCheckEvent(),
                CancellationToken.None);
            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    });
```

---

## Best Practices

1. **Always use correlation IDs** for distributed tracing
2. **Include all context** needed by event consumers
3. **Use ReservationId as partition key** to maintain order per reservation
4. **Handle duplicates** idempotently in consumers
5. **Log event publishing** for observability
6. **Monitor publishing latency** for SLO tracking
7. **Test with InMemoryEventPublisher** during development
8. **Validate Kafka configuration** before production deployment

---

## Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "Kafka not found" | Kafka:Enabled false but code expects Kafka | Check appsettings configuration |
| Duplicate events | Producer retried after partial success | Ensure idempotent consumer |
| Event ordering lost | Different partition keys for same entity | Use ReservationId as key |
| High latency | Batch size too small | Increase LingerMs in config |
| Memory usage | Buffering pending messages | Reduce BufferMemory in config |

---

## Related Documentation

- [EVENT_DRIVEN_ARCHITECTURE.md](EVENT_DRIVEN_ARCHITECTURE.md) - Complete implementation guide
- [EVENT_DRIVEN_IMPLEMENTATION.md](EVENT_DRIVEN_IMPLEMENTATION.md) - Summary and phases
- [ARCHITECTURE.md](ARCHITECTURE.md) - Clean Architecture patterns
- [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) - Development workflow

---

**Last Updated**: January 24, 2026
