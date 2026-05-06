using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reservation.Application.Events;

namespace Reservation.Infrastructure.Messaging;

/// <summary>
/// Extension methods for registering messaging services.
/// Provides flexible configuration for Kafka or in-memory event publishing.
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers messaging services based on configuration.
    /// If Kafka is enabled, configures Kafka producer. Otherwise uses in-memory publisher.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Kafka settings
        services.Configure<KafkaSettings>(configuration.GetSection(KafkaSettings.Section));

        var kafkaSettings = new KafkaSettings();
        configuration.GetSection(KafkaSettings.Section).Bind(kafkaSettings);

        // Validate settings
        if (!kafkaSettings.Validate(out var errors))
        {
            var errorMessage = $"Invalid Kafka configuration: {string.Join(", ", errors)}";
            throw new InvalidOperationException(errorMessage);
        }

        if (kafkaSettings.Enabled)
        {
            // Register Kafka producer and event publisher
            RegisterKafkaProducer(services, kafkaSettings);
            services.AddScoped<IEventPublisher, KafkaEventPublisher>();
        }
        else
        {
            // Register in-memory event publisher for development/testing
            services.AddScoped<IEventPublisher, InMemoryEventPublisher>();
        }

        return services;
    }

    /// <summary>
    /// Registers Kafka producer with the service collection.
    /// </summary>
    private static void RegisterKafkaProducer(
        IServiceCollection services,
        KafkaSettings kafkaSettings)
    {
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<KafkaProducerFactory>>();

            var producerConfig = new Confluent.Kafka.ProducerConfig
            {
                BootstrapServers = kafkaSettings.BootstrapServers,
                ClientId = "reservation-api-producer",
                Acks = Enum.Parse<Confluent.Kafka.Acks>(kafkaSettings.Acks),
                Retries = kafkaSettings.Retries,
                RetryBackoffMs = kafkaSettings.RetryBackoffMs,
                RequestTimeoutMs = kafkaSettings.RequestTimeoutMs,
                MessageFormatVersion = kafkaSettings.MessageFormatVersion,
                CompressionType = Enum.Parse<Confluent.Kafka.CompressionType>(kafkaSettings.CompressionType),
                MaxInFlightRequests = kafkaSettings.MaxInFlightRequests,
                EnableIdempotence = kafkaSettings.EnableIdempotence,
                TransactionalId = kafkaSettings.TransactionalId,
                BatchSize = kafkaSettings.BatchSize,
                LingerMs = kafkaSettings.LingerMs,
                BufferMemory = kafkaSettings.BufferMemory,
            };

            // Configure SASL if enabled
            if (kafkaSettings.SaslEnabled)
            {
                producerConfig.SecurityProtocol = Confluent.Kafka.SecurityProtocol.SaslSsl;
                producerConfig.SaslMechanism = Enum.Parse<Confluent.Kafka.SaslMechanism>(kafkaSettings.SaslMechanism);
                producerConfig.SaslUsername = kafkaSettings.SaslUsername;
                producerConfig.SaslPassword = kafkaSettings.SaslPassword;
            }

            // Configure SSL if enabled
            if (kafkaSettings.SslEnabled)
            {
                producerConfig.SslCaLocation = kafkaSettings.SslCaLocation;
            }

            logger.LogInformation(
                "Creating Kafka producer with BootstrapServers: {BootstrapServers}, Acks: {Acks}, Idempotence: {Idempotence}",
                kafkaSettings.BootstrapServers,
                kafkaSettings.Acks,
                kafkaSettings.EnableIdempotence);

            return new Confluent.Kafka.ProducerBuilder<string, string>(producerConfig).Build();
        });
    }
}

/// <summary>
/// Factory for creating Kafka producers
/// </summary>
internal class KafkaProducerFactory
{
}
