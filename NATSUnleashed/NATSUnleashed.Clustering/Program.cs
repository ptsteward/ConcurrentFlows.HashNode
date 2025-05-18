using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

// Single Node
var opts = new NatsOpts
{
    Url = "nats://localhost:4222",
    LoggerFactory = loggerFactory
};

await using var connection = new NatsConnection(opts);

var rtt = await connection.PingAsync(CancellationToken.None);

logger.LogInformation("Ping successful - {RTT}ms to {@ServerInfo}",
    rtt.TotalMilliseconds,
    connection.ServerInfo);

//// Three Node Cluster
//var opts_3node = new NatsOpts
//{
//    Url = "nats://server1:4222,nats://server2:4222,nats://server3:4222",
//    LoggerFactory = loggerFactory
//};

//var connection = await GetConnectionAsync(logger, opts);
//var stream = await GetStreamAsync(logger, connection);

//async Task<INatsConnection> GetConnectionAsync(ILogger logger, NatsOpts opts)
//{
//    await using var connection = new NatsConnection(opts);

//    var rtt = await connection.PingAsync(CancellationToken.None);

//    logger.LogInformation("Ping successful - {RTT}ms to {@ServerInfo}",
//        rtt.TotalMilliseconds,
//        connection.ServerInfo);
//    return connection;
//}

//async Task<INatsJSStream> GetStreamAsync(ILogger logger, INatsConnection connection)
//{
//    var context = new NatsJSContext(connection);
//    var config = new StreamConfig("my-first-stream", ["some.subjects.>"])
//    {
//        NumReplicas = 3
//    };

//    var stream = await context.CreateOrUpdateStreamAsync(config, CancellationToken.None);
//    logger.LogInformation("Stream created/updated - {@StreamInfo}", stream.Info);
//    return stream;
//}

//// Three Region Supercluster
//using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
//await Task.WhenAll(WestJetStreamPublishAsync(cts.Token), EastJetStreamSubscribeAsync(cts.Token));

//async Task WestJetStreamPublishAsync(CancellationToken token)
//{
//    var opts = new NatsOpts
//    {
//        Url = "nats://west1:4222,nats://west2:4222,nats://west3:4222", // Connect to local West cluster
//        LoggerFactory = loggerFactory
//    };
//    await using var connection = new NatsConnection(opts);
//    var context = new NatsJSContext(connection);

//    var streamConfig = new StreamConfig("west-stream", ["west.>"])
//    {
//        NumReplicas = 3
//    };
//    var _ = await context.CreateOrUpdateStreamAsync(streamConfig, token);

//    foreach (var id in Enumerable.Range(1, 10))
//        await context.PublishAsync("west.data", $"From West: {id}", cancellationToken: token);

//    logger.LogInformation("West client completed publishing");
//}

//async Task EastJetStreamSubscribeAsync(CancellationToken token)
//{
//    var opts = new NatsOpts
//    {
//        Url = "nats://east1:4222,nats://east2:4222, nats://east3:4222",
//        LoggerFactory = loggerFactory
//    };
//    await using var connection = new NatsConnection(opts);
//    var context = new NatsJSContext(connection);

//    SubjectTransform transform = new() { Src = "west.>", Dest = "east.fromWest.>" };
//    StreamSource source = new()
//    {
//        Name = "west-stream",
//        SubjectTransforms = [transform]
//    };
//    var streamConfig = new StreamConfig("east-stream", ["east.>"])
//    {
//        NumReplicas = 3,
//        Sources = [source]
//    };
//    var stream = await context.CreateOrUpdateStreamAsync(streamConfig, token);
//    var consumer = await stream.CreateOrderedConsumerAsync(cancellationToken: token);

//    await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: token))
//    {
//        logger.LogInformation("East client received: {Message}", msg.Data);
//        await msg.AckAsync();
//    }
//}

//// Leaf Node
//var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
//var logger = loggerFactory.CreateLogger<Program>();

//var opts = new NatsOpts
//{
//    Url = "nats://leaf:leaf@leaf-node:4222",
//    LoggerFactory = loggerFactory
//};

//await using var connection = new NatsConnection(opts);
//var context = new NatsJSContext(connection);

//var streamConfig = new StreamConfig("local-stream", ["leaf.>"]);
//var stream = await context.CreateStreamAsync(streamConfig);
//var consumer = await stream.CreateOrderedConsumerAsync();

//await context.PublishAsync<string>("leaf.data", "Hello World");

//await foreach (var msg in consumer.ConsumeAsync<string>())
//{
//    logger.LogInformation("Leaf received: {Message}", msg.Data);
//    await msg.AckAsync();
//}

//await connection.PublishAsync("remote.data", "Message to Remote");
