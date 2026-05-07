namespace KafkaConsumerService.Models;

/// <summary>
/// Configuration settings for Kafka consumer
/// </summary>
public class KafkaConsumerSettings
{
    /// <summary>
    /// Kafka bootstrap servers (comma-separated)
    /// </summary>
    public string BootstrapServers { get; set; }

    /// <summary>
    /// Topic to subscribe to
    /// </summary>
    public string Topic { get; set; }

    /// <summary>
    /// Consumer group ID
    /// </summary>
    public string GroupId { get; set; }

    /// <summary>
    /// Auto offset reset strategy
    /// </summary>
    public string AutoOffsetReset { get; set; } = "Earliest";
}
