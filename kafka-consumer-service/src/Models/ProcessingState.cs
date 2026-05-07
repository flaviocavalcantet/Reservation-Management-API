using System;

namespace KafkaConsumerService.Models
{
    public class ProcessingState
    {
        public string MessageId { get; set; }
        public bool IsProcessed { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }

        public ProcessingState(string messageId)
        {
            MessageId = messageId;
            IsProcessed = false;
            ErrorMessage = null;
            ProcessedAt = DateTime.MinValue;
        }

        public void MarkAsProcessed()
        {
            IsProcessed = true;
            ProcessedAt = DateTime.UtcNow;
        }

        public void MarkAsFailed(string errorMessage)
        {
            IsProcessed = false;
            ErrorMessage = errorMessage;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}