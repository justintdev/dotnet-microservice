using System.Text.Json;
using Application.Interfaces;
using Confluent.Kafka;
using Domain.Entities;
using Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Messaging;

public sealed class KafkaCatalogEventProducer : IEventPublisher
{
    private readonly IProducer<Null, string> _producer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly ILogger<KafkaCatalogEventProducer> _logger;

    public KafkaCatalogEventProducer(
        IProducer<Null, string> producer,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<KafkaCatalogEventProducer> logger)
    {
        _producer = producer;
        _kafkaOptions = kafkaOptions;
        _logger = logger;
    }

    public async Task PublishCatalogItemCreatedAsync(CatalogItem item, CancellationToken cancellationToken = default)
    {
        var message = JsonSerializer.Serialize(new
        {
            EventType = "CatalogItemCreated",
            Item = item
        });

        var result = await _producer.ProduceAsync(
            _kafkaOptions.Value.Topic,
            new Message<Null, string> { Value = message },
            cancellationToken);

        _logger.LogInformation(
            "Kafka published catalog item event. Topic={Topic} Partition={Partition} Offset={Offset} CatalogItemId={CatalogItemId}",
            result.Topic,
            result.Partition.Value,
            result.Offset.Value,
            item.Id);
    }
}
