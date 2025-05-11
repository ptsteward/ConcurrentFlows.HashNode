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

try
{
    await using var connection = new NatsConnection(opts);

    var rtt = await connection.PingAsync(CancellationToken.None);

    logger.LogInformation("Ping successful - {RTT}ms to {@ServerInfo}",
        rtt.TotalMilliseconds,
        connection.ServerInfo);
}
catch (NatsException ex)
{
    logger.LogError("Error connecting to NATS - {Exception}", ex);
}

// Three Node Cluster
var opts_3node = new NatsOpts
{
    Url = "nats://server1:4222,nats://server2:4222,nats://server3:4222",
    LoggerFactory = loggerFactory
};

var connection = await YieldNatsConnection(logger, opts);
var stream = await YieldStream(logger, connection);

async ValueTask<INatsConnection> YieldNatsConnection(ILogger logger, NatsOpts opts)
{
    try
    {
        await using var connection = new NatsConnection(opts);

        var rtt = await connection.PingAsync(CancellationToken.None);

        logger.LogInformation("Ping successful - {RTT}ms to {@ServerInfo}",
            rtt.TotalMilliseconds,
            connection.ServerInfo);
        return connection;
    }
    catch (NatsException ex)
    {
        logger.LogError("Error connecting to NATS - {Exception}", ex);
        throw;
    }
}

async ValueTask<INatsJSStream> YieldStream(ILogger logger, INatsConnection connection)
{
    try
    {
        var context = new NatsJSContext(connection);
        var config = new StreamConfig("my-first-stream", ["some.subjects.>"])
        {
            NumReplicas = 3
        };

        var stream = await context.CreateOrUpdateStreamAsync(config, CancellationToken.None);
        logger.LogInformation("Stream created/updated - {@StreamInfo}", stream.Info);
        return stream;
    }
    catch (NatsJSApiException ex)
    {
        logger.LogError("JetStream API Failure - {Exception}", ex);
        throw;
    }
    catch (NatsJSException ex)
    {
        logger.LogError("Stream Create/Update Failure - {Exception}", ex);
        throw;
    }
}

// Three Region Supercluster
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await Task.WhenAll(WestJetStreamPublishAsync(cts.Token), EastJetStreamSubscribeAsync(cts.Token));

async Task WestJetStreamPublishAsync(CancellationToken token)
{
    var opts = new NatsOpts
    {
        Url = "nats://west1:4222,nats://west2:4222,nats://west3:4222", // Connect to local West cluster
        LoggerFactory = loggerFactory
    };
    await using var connection = new NatsConnection(opts);
    var context = new NatsJSContext(connection);

    var streamConfig = new StreamConfig("west-stream", ["west.>"])
    {
        NumReplicas = 3
    };
    var _ = await context.CreateOrUpdateStreamAsync(streamConfig, token);

    foreach (var id in Enumerable.Range(1, 10))
        await context.PublishAsync(
            "west.data",
            $"Message: {id} - From West",
            cancellationToken: token);

    logger.LogInformation("West client completed publishing");
}

async Task EastJetStreamSubscribeAsync(CancellationToken token)
{
    var opts = new NatsOpts
    {
        Url = "nats://east1:4222,nats://east2:4222, nats://east3:4222",
        LoggerFactory = loggerFactory
    };
    await using var connection = new NatsConnection(opts);
    var context = new NatsJSContext(connection);

    var streamConfig = new StreamConfig("east-stream", ["east.>"])
    {
        NumReplicas = 3,
        Sources = [
            new() {
                Name = "west-stream",
                SubjectTransforms = [
                    new() {
                        Src = "west.>",
                        Dest = "east.fromWest.>"
                    }
                ]
            }
        ]
    };
    var stream = await context.CreateOrUpdateStreamAsync(streamConfig, token);
    var consumer = await stream.CreateOrderedConsumerAsync(cancellationToken: token);

    await foreach (var msg in consumer.ConsumeAsync<string>(cancellationToken: token))
    {
        logger.LogInformation("East client received: {Message}", msg.Data);
        await msg.AckAsync();
    }
}
