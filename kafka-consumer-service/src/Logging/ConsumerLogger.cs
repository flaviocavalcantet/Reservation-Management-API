using System;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerService.Logging
{
    public class ConsumerLogger
    {
        private readonly ILogger<ConsumerLogger> _logger;

        public ConsumerLogger(ILogger<ConsumerLogger> logger)
        {
            _logger = logger;
        }

        public void LogProcessingStart(string messageId)
        {
            _logger.LogInformation($"Processing started for message ID: {messageId}");
        }

        public void LogProcessingSuccess(string messageId)
        {
            _logger.LogInformation($"Processing succeeded for message ID: {messageId}");
        }

        public void LogProcessingFailure(string messageId, Exception ex)
        {
            _logger.LogError(ex, $"Processing failed for message ID: {messageId}");
        }

        public void LogRetry(string messageId, int retryCount)
        {
            _logger.LogWarning($"Retrying processing for message ID: {messageId}. Attempt: {retryCount}");
        }

        public void LogInfo(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                _logger.LogError(ex, message);
            }
            else
            {
                _logger.LogError(message);
            }
        }
    }
}