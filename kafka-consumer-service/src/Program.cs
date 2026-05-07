using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KafkaConsumerService.Services;
using KafkaConsumerService.Infrastructure;
using KafkaConsumerService.Models;
using KafkaConsumerService.Logging;

namespace KafkaConsumerService;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Load configuration
                var kafkaConfig = hostContext.Configuration.GetSection("Kafka");
                var retryConfig = hostContext.Configuration.GetSection("RetryPolicy");
                
                services.Configure<KafkaConsumerSettings>(kafkaConfig);
                
                var maxRetries = retryConfig.GetValue("MaxRetryAttempts", 5);
                var delayString = retryConfig.GetValue("DelayBetweenRetries", "00:00:02");
                var delay = TimeSpan.Parse(delayString);
                
                // Register services
                services.AddHostedService<KafkaConsumerHostedService>();
                services.AddScoped<IReservationEventProcessor, ReservationEventProcessor>();
                services.AddSingleton<IIdempotencyStore, IdempotencyStore>();
                services.AddSingleton<IRetryPolicy>(new RetryPolicy(maxRetries, delay));
                services.AddSingleton<ConsumerLogger>();
                services.AddLogging();
            });
}