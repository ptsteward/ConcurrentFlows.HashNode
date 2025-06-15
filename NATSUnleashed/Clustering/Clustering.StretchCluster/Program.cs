using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using System.Text.Json;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var subscribing = Region2LocalStreamSubscribeAsync(cts.Token);
var publishing = StretchStreamPublishAsync(cts.Token);

await Task.WhenAll(publishing, subscribing);

await StreamReportAsync(cts.Token);

async Task StretchStreamPublishAsync(CancellationToken token)
{
    var context = GetJetStreamContext();
    var streamConfig = new StreamConfig("stretch-stream", ["all-regions.>"])
    {
        NumReplicas = 3,
    };
    _ = await context.CreateOrUpdateStreamAsync(streamConfig, token);    

    foreach (var id in Enumerable.Range(1, 10))
        await context.PublishAsync("all-regions.data", $"Stretch Msg: {id}", cancellationToken: token);

    logger.LogInformation("Stretch client completed publishing");
}

async Task Region2LocalStreamSubscribeAsync(CancellationToken token)
{
    var context = GetJetStreamContext();
    Placement placement = new() { Tags = ["region:region2"] };
    SubjectTransform transform = new() { Src = "all-regions.>", Dest = "region2.duplicate.>" };
    StreamSource source = new()
    {
        Name = "stretch-stream",
        SubjectTransforms = [transform]
    };
    var streamConfig = new StreamConfig("region2-stream", ["region2.>"])
    {
        NumReplicas = 3,
        Placement = placement,
        Sources = [source]
    };
    var stream = await context.CreateOrUpdateStreamAsync(streamConfig, token);
    var consumer = await stream.CreateOrderedConsumerAsync(cancellationToken: token);

    await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: token))
    {
        logger.LogInformation("Region2 client received: {Message}", msg.Data);
        await msg.AckAsync(cancellationToken: token);
    }
}

async Task StreamReportAsync(CancellationToken token)
{    
    var context = GetJetStreamContext();
    var opts = new JsonSerializerOptions() { WriteIndented = true };    
    await foreach (var stream in context.ListStreamsAsync(cancellationToken: token))
    {
        var name = stream.Info.Config.Name;
        var info = JsonSerializer.Serialize(stream.Info, opts);
        logger.LogInformation(@"
{Name} Info:
{Info}", name, info);
    }
}

INatsJSContext GetJetStreamContext()
    => new NatsJSContext(BuildNatsConnection());

INatsConnection BuildNatsConnection()
    => new NatsConnection(new()
    {
        Url = $"nats://region1:4222,nats://region2:4222,nats://region3:4222",
        LoggerFactory = loggerFactory
    });
