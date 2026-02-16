using Confluent.Kafka;
using Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging;

public sealed class KafkaCatalogEventsConsumer : BackgroundService
{
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly ILogger<KafkaCatalogEventsConsumer> _logger;

    public KafkaCatalogEventsConsumer(
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<KafkaCatalogEventsConsumer> logger)
    {
        _kafkaOptions = kafkaOptions;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _kafkaOptions.Value;

        if (string.IsNullOrWhiteSpace(options.BootstrapServers) || string.IsNullOrWhiteSpace(options.Topic))
        {
            _logger.LogWarning("Kafka consumer not started because bootstrap server/topic configuration is missing.");
            return Task.CompletedTask;
        }

        return Task.Run(() => ConsumeLoop(options, stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(KafkaOptions options, CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(options.Topic);

        _logger.LogInformation("Kafka consumer subscribed to topic {KafkaTopic}", options.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null)
                {
                    continue;
                }

                _logger.LogInformation(
                    "Kafka message consumed. Topic={Topic} Partition={Partition} Offset={Offset} Value={Message}",
                    result.Topic,
                    result.Partition.Value,
                    result.Offset.Value,
                    result.Message.Value);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer stopping due to cancellation request.");
        }
        finally
        {
            consumer.Close();
        }
    }
}
