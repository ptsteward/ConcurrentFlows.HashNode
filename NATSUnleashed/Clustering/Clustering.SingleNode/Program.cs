using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

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


//// Three Region Supercluster


//// Leaf Node

