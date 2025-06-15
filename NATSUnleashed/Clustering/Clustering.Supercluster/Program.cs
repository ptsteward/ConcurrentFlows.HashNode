using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Client.JetStream;

const string Region1 = "region1";
const string Region2 = "region2";
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var subscribing = Region2JetStreamSubscribeAsync(cts.Token);
var publishing = Region1JetStreamPublishAsync(cts.Token);

await Task.WhenAll(publishing, subscribing);

async Task Region1JetStreamPublishAsync(CancellationToken token)
{
    var context = GetRegion1JetStreamContext();
    Placement placement = new() { Cluster = Region1 };
    var streamConfig = new StreamConfig($"{Region1}-stream", [$"{Region1}.>"])
    {
        NumReplicas = 3,
        Placement = placement
    };
    _ = await context.CreateOrUpdateStreamAsync(streamConfig, token);

    foreach (var id in Enumerable.Range(1, 10))
        await context.PublishAsync($"{Region1}.data", $"From {Region1}: {id}", cancellationToken: token);

    logger.LogInformation("{Region} client completed publishing", Region1);
}

async Task Region2JetStreamSubscribeAsync(CancellationToken token)
{
    var context = GetRegion2JetStreamContext();
    Placement placement = new() { Cluster = Region2 };
    SubjectTransform transform = new() { Src = $"{Region1}.>", Dest = $"{Region2}.from-{Region1}.>" };
    StreamSource source = new()
    {
        Name = $"{Region1}-stream",
        SubjectTransforms = [transform]
    };
    var streamConfig = new StreamConfig($"{Region2}-stream", [$"{Region2}.>"])
    {
        NumReplicas = 3,
        Placement = placement,
        Sources = [source]
    };
    var stream = await context.CreateOrUpdateStreamAsync(streamConfig, token);
    var consumer = await stream.CreateOrderedConsumerAsync(cancellationToken: token);

    await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: token))
    {
        logger.LogInformation("{Region} client received: {Message}", Region2, msg.Data);
        await msg.AckAsync(cancellationToken: token);
    }
}

INatsJSContext GetRegion1JetStreamContext()
    => new NatsJSContext(BuildNatsConnection(Region1));

INatsJSContext GetRegion2JetStreamContext()
    => new NatsJSContext(BuildNatsConnection(Region2));

INatsConnection BuildNatsConnection(string region)
    => new NatsConnection(new()
    {
        Url = $"nats://{region}1:4222,nats://{region}2:4222,nats://{region}3:4222",
        LoggerFactory = loggerFactory
    });
