namespace Infrastructure.Configuration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = "localhost:9092";
    public string Topic { get; init; } = "catalog-items";
    public string GroupId { get; init; } = "catalog-service-group";
}
