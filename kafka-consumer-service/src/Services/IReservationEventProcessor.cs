using System.Threading.Tasks;
using KafkaConsumerService.Models;

namespace KafkaConsumerService.Services
{
    public interface IReservationEventProcessor
    {
        Task ProcessAsync(string messageJson);
    }
}