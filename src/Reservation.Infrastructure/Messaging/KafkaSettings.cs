namespace Reservation.Infrastructure.Messaging;

/// <summary>
/// Configuration options for Kafka messaging.
/// Supports flexible deployment scenarios: production Kafka, in-memory testing, or custom implementations.
/// </summary>
public class KafkaSettings
{
    /// <summary>
    /// Gets or sets the section name in configuration
    /// </summary>
    public const string Section = "Kafka";

    /// <summary>
    /// Comma-separated list of Kafka broker addresses (e.g., "localhost:9092,localhost:9093")
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Consumer group ID for message consumption
    /// </summary>
    public string GroupId { get; set; } = "reservation-api";

    /// <summary>
    /// Message format version for producer
    /// </summary>
    public string MessageFormatVersion { get; set; } = "2.1";

    /// <summary>
    /// Compression type: none, gzip, snappy, lz4, zstd
    /// </summary>
    public string CompressionType { get; set; } = "snappy";

    /// <summary>
    /// Number of in-flight requests per broker connection
    /// Higher value = better parallelism, lower value = better ordering guarantees
    /// </summary>
    public int MaxInFlightRequests { get; set; } = 5;

    /// <summary>
    /// Request timeout in milliseconds
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Acks required for producer: 0 (none), 1 (leader), -1 (all)
    /// -1 means all replicas must acknowledge (stronger durability guarantee)
    /// </summary>
    public string Acks { get; set; } = "all";

    /// <summary>
    /// Number of retries before giving up on a message
    /// </summary>
    public int Retries { get; set; } = 3;

    /// <summary>
    /// Backoff time between retries in milliseconds
    /// </summary>
    public int RetryBackoffMs { get; set; } = 100;

    /// <summary>
    /// Enable idempotent producer (guarantees each message sent exactly once)
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// Transactional ID for exactly-once delivery semantics
    /// Set to null to disable transactions
    /// </summary>
    public string? TransactionalId { get; set; }

    /// <summary>
    /// Batch size in bytes for grouping messages
    /// </summary>
    public int BatchSize { get; set; } = 16384;

    /// <summary>
    /// Linger time in milliseconds before sending a batch
    /// Increases throughput by reducing number of requests
    /// </summary>
    public int LingerMs { get; set; } = 10;

    /// <summary>
    /// Buffer memory in bytes for pending messages
    /// </summary>
    public long BufferMemory { get; set; } = 33554432; // 32MB

    /// <summary>
    /// Determines whether to use Kafka or in-memory publisher
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Enable SASL authentication
    /// </summary>
    public bool SaslEnabled { get; set; } = false;

    /// <summary>
    /// SASL mechanism: PLAIN, SCRAM-SHA-256, SCRAM-SHA-512
    /// </summary>
    public string SaslMechanism { get; set; } = "PLAIN";

    /// <summary>
    /// SASL username
    /// </summary>
    public string? SaslUsername { get; set; }

    /// <summary>
    /// SASL password
    /// </summary>
    public string? SaslPassword { get; set; }

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool SslEnabled { get; set; } = false;

    /// <summary>
    /// Path to CA certificate file
    /// </summary>
    public string? SslCaLocation { get; set; }

    /// <summary>
    /// Validates the Kafka settings
    /// </summary>
    public bool Validate(out IEnumerable<string> errors)
    {
        var errorList = new List<string>();

        if (string.IsNullOrWhiteSpace(BootstrapServers))
        {
            errorList.Add("BootstrapServers cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(GroupId))
        {
            errorList.Add("GroupId cannot be empty");
        }

        if (EnableIdempotence && MaxInFlightRequests > 5)
        {
            errorList.Add("When idempotence is enabled, MaxInFlightRequests must be <= 5");
        }

        if (RequestTimeoutMs < 1000)
        {
            errorList.Add("RequestTimeoutMs must be at least 1000ms");
        }

        errors = errorList;
        return !errorList.Any();
    }
}
