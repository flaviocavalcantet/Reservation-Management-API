using System.Threading.Tasks;

namespace KafkaConsumerService.Infrastructure
{
    public interface IIdempotencyStore
    {
        Task<bool> ExistsAsync(string idempotencyKey);
        Task StoreAsync(string idempotencyKey);
    }
}