using System;
using System.Threading.Tasks;

namespace KafkaConsumerService.Infrastructure;

public class RetryPolicy : IRetryPolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _delay;

    public RetryPolicy(int maxRetries, TimeSpan delay)
    {
        _maxRetries = maxRetries;
        _delay = delay;
    }

    public async Task ExecuteAsync(Func<Task> action)
    {
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                await action();
                return; // Success, exit the method
            }
            catch
            {
                if (attempt == _maxRetries - 1)
                {
                    throw; // Rethrow the last exception
                }
                await Task.Delay(_delay); // Wait before retrying
            }
        }
    }
}