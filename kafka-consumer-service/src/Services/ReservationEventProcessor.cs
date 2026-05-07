using System;
using System.Text.Json;
using System.Threading.Tasks;
using KafkaConsumerService.Infrastructure;
using KafkaConsumerService.Models;
using KafkaConsumerService.Logging;

namespace KafkaConsumerService.Services;

public class ReservationEventProcessor : IReservationEventProcessor
{
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ConsumerLogger _logger;

    public ReservationEventProcessor(IIdempotencyStore idempotencyStore, IRetryPolicy retryPolicy, ConsumerLogger logger)
    {
        _idempotencyStore = idempotencyStore;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task ProcessAsync(string messageJson)
    {
        try
        {
            var reservationEvent = JsonSerializer.Deserialize<ReservationEvent>(messageJson);
            
            if (reservationEvent == null)
            {
                _logger.LogProcessingFailure("unknown", new ArgumentException("Failed to deserialize message"));
                return;
            }

            if (await _idempotencyStore.ExistsAsync(reservationEvent.Id))
            {
                _logger.LogProcessingFailure(reservationEvent.Id, new Exception("Event has already been processed. Skipping."));
                return;
            }

            _logger.LogProcessingStart(reservationEvent.Id);

            await _retryPolicy.ExecuteAsync(async () =>
            {
                // Process the reservation event
                await HandleReservation(reservationEvent);

                // Mark the event as processed
                await _idempotencyStore.StoreAsync(reservationEvent.Id);
                _logger.LogProcessingSuccess(reservationEvent.Id);
            });
        }
        catch (Exception ex)
        {
            _logger.LogProcessingFailure(messageJson, ex);
            throw; // Rethrow to trigger retry
        }
    }

    private Task HandleReservation(ReservationEvent reservationEvent)
    {
        // Implement the logic to handle the reservation event
        // This could involve updating a database, sending notifications, etc.
        return Task.CompletedTask;
    }
}