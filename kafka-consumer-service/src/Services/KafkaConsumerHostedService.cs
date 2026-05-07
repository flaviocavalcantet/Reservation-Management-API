using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KafkaConsumerService.Services;
using KafkaConsumerService.Models;

namespace KafkaConsumerService.Services;

public class KafkaConsumerHostedService : IHostedService
{
    private readonly IReservationEventProcessor _eventProcessor;
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private readonly IOptions<KafkaConsumerSettings> _kafkaSettings;
    private IConsumer<Ignore, string> _consumer;

    public KafkaConsumerHostedService(IReservationEventProcessor eventProcessor, ILogger<KafkaConsumerHostedService> logger, IOptions<KafkaConsumerSettings> kafkaSettings)
    {
        _eventProcessor = eventProcessor;
        _logger = logger;
        _kafkaSettings = kafkaSettings;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.Value.BootstrapServers,
            GroupId = _kafkaSettings.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe(_kafkaSettings.Value.Topic);

        Task.Run(() => ConsumeMessages(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ConsumeMessages(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var cr = _consumer.Consume(cancellationToken);
                    await _eventProcessor.ProcessAsync(cr.Message.Value);
                }
                catch (ConsumeException e)
                {
                    _logger.LogError(e.Error.Reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing the message.");
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Close();
        return Task.CompletedTask;
    }
}