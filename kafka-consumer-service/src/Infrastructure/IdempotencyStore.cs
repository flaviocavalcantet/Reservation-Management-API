using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace KafkaConsumerService.Infrastructure;

public class IdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, bool> _store = new();

    public Task<bool> ExistsAsync(string idempotencyKey)
    {
        return Task.FromResult(_store.ContainsKey(idempotencyKey));
    }

    public Task StoreAsync(string idempotencyKey)
    {
        _store.TryAdd(idempotencyKey, true);
        return Task.CompletedTask;
    }
}