using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream.Models;
using NATS.Client.JetStream;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

var opts = new NatsOpts
{
    Url = "nats://server1:4222,nats://server2:4222,nats://server3:4222",
    LoggerFactory = loggerFactory
};

var connection = await GetConnectionAsync(logger, opts);
var stream = await GetStreamAsync(logger, connection);

async Task<INatsConnection> GetConnectionAsync(ILogger logger, NatsOpts opts)
{
    await using var connection = new NatsConnection(opts);

    var rtt = await connection.PingAsync(CancellationToken.None);

    logger.LogInformation("Ping successful - {RTT}ms to {@ServerInfo}",
        rtt.TotalMilliseconds,
        connection.ServerInfo);
    return connection;
}

async Task<INatsJSStream> GetStreamAsync(ILogger logger, INatsConnection connection)
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
