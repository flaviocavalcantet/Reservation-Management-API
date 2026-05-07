using System;

namespace KafkaConsumerService.Models
{
    public class ReservationEvent
    {
        public string Id { get; set; }
        public string ReservationId { get; set; }
        public string CustomerId { get; set; }
        public DateTime ReservationDate { get; set; }
        public string Status { get; set; }
    }
}