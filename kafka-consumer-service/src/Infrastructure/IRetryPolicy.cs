using System;
using System.Threading.Tasks;

namespace KafkaConsumerService.Infrastructure
{
    public interface IRetryPolicy
    {
        Task ExecuteAsync(Func<Task> action);
    }
}