using BenchmarkDotNet.Attributes;
using Bogus;
using ConcurrentFlows.KafkaProducer;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace ConcurrentFlows.KafkaProducer1;

[MemoryDiagnoser]
[MarkdownExporter]
public class ProducerBenchmarks
{
    private readonly IProducer<string, WidgetEvent> asyncProducer;
    private readonly IProducer<string, WidgetEvent> syncProducer;

    private readonly string sync_topic = nameof(sync_topic);
    private readonly string async_topic = nameof(async_topic);
    private readonly Faker<WidgetEvent> faker = new();
    private readonly string errorMessage = @"Error:
Code - {0}
Reason - {1}";

    public ProducerBenchmarks()
    {
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = "localhost:9092,localhost:9093,localhost:9094",
            QueueBufferingMaxMessages = 500_000
        };

        var registryConfig = new SchemaRegistryConfig()
        {
            Url = "localhost:8081",
        };
        var registryClient = new CachedSchemaRegistryClient(registryConfig);

        syncProducer = new ProducerBuilder<string, WidgetEvent>(producerConfig)
            .SetValueSerializer(
                new ProtobufSerializer<WidgetEvent>(registryClient)
                .AsSyncOverAsync())
            .SetErrorHandler((p, e)
                => Console.WriteLine(
                    errorMessage, e.Code, e.Reason))
            .Build();

        asyncProducer = new ProducerBuilder<string, WidgetEvent>(producerConfig)
            .SetValueSerializer(
                new ProtobufSerializer<WidgetEvent>(registryClient))
            .SetErrorHandler((p, e)
                => Console.WriteLine(
                    errorMessage, e.Code, e.Reason))
            .Build();
    }

    [Benchmark]
    public void KafkaProducerSync()
    {
        var msg = new Message<string, WidgetEvent>()
        {
            Key = $"{Guid.NewGuid()}",
            Value = faker.Generate()
        };
        syncProducer.Produce(sync_topic, msg,
            d =>
            {
                if (d.Error.IsError)
                    throw new InvalidOperationException(
                        $"{d.Error.Code}:{d.Error.Reason}");
            });
    }

    [Benchmark]
    public async Task KafkaProducerAsync()
    {
        var msg = new Message<string, WidgetEvent>()
        {
            Key = $"{Guid.NewGuid()}",
            Value = faker.Generate()
        };
        await asyncProducer.ProduceAsync(async_topic, msg);
    }
}
